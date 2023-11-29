using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using API.Util;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Service.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace API.Tasks;

public class UserRefresh(
    IServiceProvider services,
    ILogger<UserRefresh> logger,
    IBungieClient bungieClient)
    : BackgroundService
{
    private const string ServiceName = "UserRefresh";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = services.CreateScope();
        var db =
            scope.ServiceProvider
                .GetRequiredService<DbManager>();

        while (!stoppingToken.IsCancellationRequested)
        {
            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = true;

            try
            {
                var users = await db.Users.Include(u => u.BungieProfiles).ToListAsync(stoppingToken);
                var bungieProfiles = users.SelectMany(u => u.BungieProfiles)
                    // TODO: .Where(u => u.NeverExpire)
                    .Where(u => u.NeedsRefresh());

                foreach (var profile in bungieProfiles)
                {
                    await profile.RefreshToken(bungieClient, DateTime.UtcNow);

                    if (profile.DestinyMembershipId == 0)
                        await profile.UpdateMembership(bungieClient);
                }

                await db.SaveChangesAsync(stoppingToken);

                foreach (var user in users)
                {
                    var userProfile =
                        user.BungieProfiles.FirstOrDefault(p => p.TokenExpires < DateTime.UtcNow);
                    if (userProfile == null)
                        continue;

                    var characterId = await bungieClient.ApiAccess.Destiny2.GetProfile(
                        userProfile.DestinyMembershipType, userProfile.DestinyMembershipId,
                        [DestinyComponentType.Characters], cancellationToken: stoppingToken);

                    var vendors = await bungieClient.ApiAccess.Destiny2.GetVendors(
                        userProfile.DestinyMembershipType, userProfile.DestinyMembershipId,
                        characterId.Response.Characters.Data.FirstOrDefault().Value.CharacterId,
                        [DestinyComponentType.Vendors], userProfile.GetTokenData(), stoppingToken);

                    if (!vendors.IsSuccessfulResponseCode)
                        continue;

                    if (vendors.Response.Vendors.Data.TryGetValue(DefinitionHashes.Vendors.LordSaladin,
                            out var bannerValue))
                    {
                        var addBannerUser = false;
                        var bannerVendorUser = await db.VendorUsers.FirstOrDefaultAsync(
                            vu => vu.UserId == user.Id && vu.VendorId == DefinitionHashes.Vendors.LordSaladin,
                            stoppingToken);

                        if (bannerVendorUser == null)
                        {
                            addBannerUser = true;
                            bannerVendorUser = new VendorUser
                            {
                                User = user,
                                VendorId = DefinitionHashes.Vendors.LordSaladin
                            };
                        }

                        bannerVendorUser.Rank = bannerValue.Progression.Level;
                        bannerVendorUser.Resets = bannerValue.Progression.CurrentResetCount ?? 0;

                        if (addBannerUser)
                            await db.VendorUsers.AddAsync(bannerVendorUser, stoppingToken);
                    }

                    // ReSharper disable once InvertIf
                    if (vendors.Response.Vendors.Data.TryGetValue(DefinitionHashes.Vendors.Saint14,
                            out var trialsValue))
                    {
                        var addTrialsUser = false;

                        var trialsVendorUser = await db.VendorUsers.FirstOrDefaultAsync(
                            vu => vu.UserId == user.Id && vu.VendorId == DefinitionHashes.Vendors.Saint14,
                            stoppingToken);

                        if (trialsVendorUser == null)
                        {
                            addTrialsUser = true;
                            trialsVendorUser = new VendorUser
                            {
                                User = user,
                                VendorId = DefinitionHashes.Vendors.Saint14
                            };
                        }

                        trialsVendorUser.Rank = trialsValue.Progression.Level;
                        trialsVendorUser.Resets = trialsValue.Progression.CurrentResetCount ?? 0;

                        if (addTrialsUser)
                            await db.VendorUsers.AddAsync(trialsVendorUser, stoppingToken);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception in {service}", ServiceName);
            }

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = false;
            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).LastRun = DateTime.UtcNow;

            await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(60), stoppingToken);
        }
    }
}
