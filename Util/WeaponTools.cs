using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Responses;
using static DotNetBungieAPI.HashReferences.DefinitionHashes.InventoryItems;

namespace API.Util;

public static class WeaponTools
{
    public static List<List<uint>> PopulatePerks(BungieResponse<DestinyVendorResponse> vendorQuery, int key)
    {
        var disallowList = new List<string>
        {
            "Intrinsic",
            "Restore Defaults",
            "Weapon Mod"
        };

        var list = new List<List<uint>>();

        if (!vendorQuery.Response.ItemComponents.ReusablePlugs.Data.TryGetValue(key, out var plugComponent))
            return list;

        foreach (var plugSet in plugComponent.Plugs)
        {
            var plugList = new List<uint>();

            plugList.AddRange(from plug in plugSet.Value
                where !disallowList.Contains(plug.PlugItem.Select(x => x.ItemTypeDisplayName))
                where !plug.PlugItem.Select(x => x.DisplayProperties.Name).Contains(" Frame")
                where !plug.PlugItem.Select(x => x.DisplayProperties.Name).Contains(" Tracker")
                select plug.PlugItem.Select(x => x.Hash));

            if (plugList.Count != 0)
                list.Add(plugList);
        }

        return list;
    }

    public static uint GetWeaponFromDummy(uint dummyHash)
    {
        return dummyHash switch
        {
            AishasEmbraceAdept_274751425 => AishasEmbraceAdept_3245493570,
            AstralHorizonAdept_2612190756 => AstralHorizonAdept_854379020,
            BrayTechOspreyAdept_2750585162 => BrayTechOspreyAdept_1064132738,
            BrayTechOspreyAdept_3341152510 => BrayTechOspreyAdept_1064132738,
            BurdenofGuiltAdept_1850049597 => BurdenofGuiltAdept_2002522739,
            BuzzardAdept_1478676677 => BuzzardAdept_927835311,
            CataphractGL3Adept_1715635757 => CataphractGL3Adept_874623537,
            CataphractGL3Adept_2765906441 => CataphractGL3Adept_874623537,
            CataphractGL3Adept_3851153643 => CataphractGL3Adept_874623537,
            CataphractGL3Adept_3896483179 => CataphractGL3Adept_874623537,
            CataphractGL3Adept_894429363 => CataphractGL3Adept_874623537,
            CataphractGL3Adept_919093444 => CataphractGL3Adept_874623537,
            ExaltedTruthAdept_559460316 => ExaltedTruthAdept_1705843397,
            EyeofSolAdept_1693824330 => EyeofSolAdept_3637570176,
            EyeofSolAdept_2177836901 => EyeofSolAdept_3637570176,
            EyeofSolAdept_2327842034 => EyeofSolAdept_3637570176,
            EyeofSolAdept_2485287928 => EyeofSolAdept_3637570176,
            EyeofSolAdept_3349188249 => EyeofSolAdept_3637570176,
            EyeofSolAdept_652245563 => EyeofSolAdept_3637570176,
            ForgivenessAdept_937919489 => ForgivenessAdept_2405619467,
            HungJurySR4Adept_3348579915 => HungJurySR4Adept_2883684343,
            IgneousHammerAdept_3198469301 => IgneousHammerAdept_2314610827,
            IgneousHammerAdept_3223133382 => IgneousHammerAdept_2314610827,
            IgneousHammerAdept_3659015538 => IgneousHammerAdept_2314610827,
            IgneousHammerAdept_3816461432 => IgneousHammerAdept_2314610827,
            IgneousHammerAdept_3888726283 => IgneousHammerAdept_2314610827,
            IgneousHammerAdept_4106707994 => IgneousHammerAdept_2314610827,
            IncisorAdept_2117949410 => IncisorAdept_2421180981,
            IncisorAdept_2241602269 => IncisorAdept_2421180981,
            IncisorAdept_242835753 => IncisorAdept_2421180981,
            IncisorAdept_3705631300 => IncisorAdept_2421180981,
            IncisorAdept_3914231265 => IncisorAdept_2421180981,
            IncisorAdept_963073439 => IncisorAdept_2421180981,
            LoadedQuestionAdept_2016746467 => LoadedQuestionAdept_2914913838,
            LoadedQuestionAdept_643164677 => LoadedQuestionAdept_2914913838,
            MindbendersAmbitionAdept_1909919170 => MindbendersAmbitionAdept_2074041946,
            PreAstyanaxIVAdept_130489153 => PreAstyanaxIVAdept_496556698,
            PreAstyanaxIVAdept_2807348229 => PreAstyanaxIVAdept_496556698,
            ReedsRegretAdept_1894611475 => ReedsRegretAdept_2475355656,
            ShayurasWrathAdept_1166076293 => ShayurasWrathAdept_4023807721,
            TheImmortalAdept_3866200400 => TheImmortalAdept_3193598749,
            TheInquisitorAdept_3364224034 => TheInquisitorAdept_825554997,
            TheMessengerAdept_1059738996 => TheMessengerAdept_1173780905,
            TheMessengerAdept_1139237504 => TheMessengerAdept_1173780905,
            TheMessengerAdept_2455457034 => TheMessengerAdept_1173780905,
            TheMessengerAdept_2785757665 => TheMessengerAdept_1173780905,
            TheMessengerAdept_3409329449 => TheMessengerAdept_1173780905,
            TheMessengerAdept_702044043 => TheMessengerAdept_1173780905,
            TheMilitiasBirthrightAdept_66009811 => TheMilitiasBirthrightAdept_4162642204,
            THESWARMAdept_221737434 => THESWARMAdept_566740455,
            UndercurrentAdept_2483503540 => UndercurrentAdept_672957262,
            UnexpectedResurgenceAdept_1638178212 => UnexpectedResurgenceAdept_1141586039,
            UnexpectedResurgenceAdept_2246184588 => UnexpectedResurgenceAdept_1141586039,
            UnexpectedResurgenceAdept_2505979445 => UnexpectedResurgenceAdept_1141586039,
            UnexpectedResurgenceAdept_3743201325 => UnexpectedResurgenceAdept_1141586039,
            UnexpectedResurgenceAdept_3805958916 => UnexpectedResurgenceAdept_1141586039,
            UnexpectedResurgenceAdept_50599490 => UnexpectedResurgenceAdept_1141586039,
            UnwaveringDutyAdept_1282930036 => UnwaveringDutyAdept_3444632029,
            UzumeRR4Adept_1256672756 => UzumeRR4Adept_1891996599,
            WardensLawAdept_1606927173 => WardensLawAdept_267089201,
            WardensLawAdept_2088917122 => WardensLawAdept_267089201,
            WendigoGL3Adept_1514776690 => WendigoGL3Adept_3915197957,
            WhistlersWhimAdept_3854555969 => WhistlersWhimAdept_161675590,
            _ => 0
        };
    }
}
