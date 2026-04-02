using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public enum CurrencyStyle
    {
        Silver = 0,
        Dollars = 1,
        Gold = 2,
        Credits = 3,
        Caps = 4,
        Septims = 5,
        Drakes = 6,
        Cats = 7
    }

    public static class CurrencyUtility
    {
        public const int OptionCount = 8;

        public static CurrencyStyle CurrentStyle
        {
            get
            {
                int index = WealthBeyondMeasureMod.Settings?.currencyStyleIndex ?? 0;
                index = Math.Max(0, Math.Min(index, OptionCount - 1));
                return (CurrencyStyle)index;
            }
        }

        public static IEnumerable<string> OptionLabels
        {
            get
            {
                yield return "WBM_Currency_Silver".Translate();
                yield return "WBM_Currency_Dollars".Translate();
                yield return "WBM_Currency_Gold".Translate();
                yield return "WBM_Currency_Credits".Translate();
                yield return "WBM_Currency_Caps".Translate();
                yield return "WBM_Currency_Septims".Translate();
                yield return "WBM_Currency_Drakes".Translate();
                yield return "WBM_Currency_Cats".Translate();
            }
        }

        public static string GetCurrentLabel()
        {
            switch (CurrentStyle)
            {
                case CurrencyStyle.Dollars:
                    return "dollars";
                case CurrencyStyle.Gold:
                    return "gold";
                case CurrencyStyle.Credits:
                    return "credits";
                case CurrencyStyle.Caps:
                    return "caps";
                case CurrencyStyle.Septims:
                    return "septims";
                case CurrencyStyle.Drakes:
                    return "drakes";
                case CurrencyStyle.Cats:
                    return "cats";
                default:
                    return "silver";
            }
        }

        public static string GetCurrentLabelCapitalized()
        {
            return GetCurrentLabel().CapitalizeFirst();
        }

        public static string GetOptionLabel(int index)
        {
            switch ((CurrencyStyle)Math.Max(0, Math.Min(index, OptionCount - 1)))
            {
                case CurrencyStyle.Dollars:
                    return "WBM_Currency_Dollars".Translate();
                case CurrencyStyle.Gold:
                    return "WBM_Currency_Gold".Translate();
                case CurrencyStyle.Credits:
                    return "WBM_Currency_Credits".Translate();
                case CurrencyStyle.Caps:
                    return "WBM_Currency_Caps".Translate();
                case CurrencyStyle.Septims:
                    return "WBM_Currency_Septims".Translate();
                case CurrencyStyle.Drakes:
                    return "WBM_Currency_Drakes".Translate();
                case CurrencyStyle.Cats:
                    return "WBM_Currency_Cats".Translate();
                default:
                    return "WBM_Currency_Silver".Translate();
            }
        }

        public static string GetTexturePath()
        {
            switch (CurrentStyle)
            {
                case CurrencyStyle.Dollars:
                    return "Things/Currency/Dollars";
                case CurrencyStyle.Gold:
                    return "Things/Currency/Gold";
                case CurrencyStyle.Credits:
                    return "Things/Currency/Credits";
                case CurrencyStyle.Caps:
                    return "Things/Currency/Caps";
                case CurrencyStyle.Septims:
                    return "Things/Currency/Septims";
                case CurrencyStyle.Drakes:
                    return "Things/Currency/Drakes";
                case CurrencyStyle.Cats:
                    return "Things/Currency/Cats";
                default:
                    return "Things/Currency/Silver";
            }
        }

        public static void ApplyCurrencyPresentation()
        {
            ThingDef currencyDef = DefDatabase<ThingDef>.GetNamedSilentFail("Silver");
            if (currencyDef == null)
            {
                return;
            }

            currencyDef.label = GetCurrentLabel();
            currencyDef.description = "WBM_CurrencyDescription".Translate(GetCurrentLabel());

            if (currencyDef.graphicData != null)
            {
                currencyDef.graphicData.texPath = GetTexturePath();
            }

            TrySetField(currencyDef, "uiIconPath", GetTexturePath());
            TrySetField(currencyDef, "graphic", null);
            TrySetField(currencyDef, "uiIcon", null);
        }

        private static void TrySetField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                return;
            }

            if (value == null && field.FieldType.IsValueType)
            {
                return;
            }

            try
            {
                field.SetValue(target, value);
            }
            catch
            {
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class CurrencyPresentationBootstrap
    {
        static CurrencyPresentationBootstrap()
        {
            LongEventHandler.ExecuteWhenFinished(CurrencyUtility.ApplyCurrencyPresentation);
        }
    }

}
