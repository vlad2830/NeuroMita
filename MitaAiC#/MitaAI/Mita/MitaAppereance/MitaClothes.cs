using Il2Cpp;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
using MelonLoader;


namespace MitaAI.MitaAppereance
{
    public class MitaClothesModded
    {
        //MitaPerson MitaPerson;
        public enum Clothes

        {
            original,
            SchoolVariant1,
            SchoolVariant2,
            SchoolVariant3,
            Chirstmass,
            Vampire
        }
        public static Clothes currentClothes = Clothes.original;
        public static void init(HarmonyLib.Harmony harmony)
        {

            MethodInfo original = HarmonyLib.AccessTools.Method(typeof(GameController), "Update");
            MethodInfo original2 = HarmonyLib.AccessTools.Method(typeof(MitaClothes), name: "Start");
            MethodInfo patch = HarmonyLib.AccessTools.Method(typeof(MitaClothesModded), "UpdateTest");
            MethodInfo patch2 = HarmonyLib.AccessTools.Method(typeof(MitaClothesModded), "Start");
            //harmony.Patch(original, new HarmonyLib.HarmonyMethod(patch));
            harmony.Patch(original2, new HarmonyLib.HarmonyMethod(patch2));
        }
        public static string ProcessClothes(string response)
        {
            MelonLogger.Msg($"Inside ProcessClothes");
            List<string> clothes = new List<string>();
            string pattern = @"<clothes>(.*?)</clothes>";
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    clothes.Add(match.Groups[1].Value);
                    MelonLogger.Msg($"Try Parse " + clothes[0]);
                    Enum.TryParse<Clothes>(clothes[0], true, out var form);

                    SetClothes(form);
                    break;
                }
            }

            string result = Regex.Replace(response, @"<clothes>.*?</clothes>", "");


            MelonLogger.Msg($"Inside ProcessClothes End");
            return result;

        }

        public static void Start(MitaClothes __instance)
        {
            __instance.dontDestroyStart = true;
        }
        public static void UpdateTest(GameController __instance)
        {
            if (Input.GetKeyInt(KeyCode.Alpha1))
            {
                SetClothes(Clothes.SchoolVariant1);
            }
            if (Input.GetKeyInt(KeyCode.Alpha2))
            {
                SetClothes(Clothes.SchoolVariant2);
            }
            if (Input.GetKeyInt(KeyCode.Alpha3))
            {
                SetClothes(Clothes.SchoolVariant3);
            }
            if (Input.GetKeyInt(KeyCode.Alpha4))
            {
                SetClothes(Clothes.Chirstmass);
            }
            if (Input.GetKeyInt(KeyCode.Alpha5))
            {
                SetClothes(Clothes.Vampire);
            }
        }
        static void SetClothes(Clothes cloth)
        {
            MelonLogger.Msg($"Try SetClothes");
            switch (cloth)
            {
                case Clothes.original:
                    GlobalGame.clothMita = "original";
                    GlobalGame.clothVariantMita = 0;
                    break;
                case Clothes.SchoolVariant1:
                    GlobalGame.clothMita = "FIIdClSchool";
                    GlobalGame.clothVariantMita = 0;
                    break;

                case Clothes.SchoolVariant2:
                    GlobalGame.clothMita = "FIIdClSchool";
                    GlobalGame.clothVariantMita = 1;
                    break;

                case Clothes.SchoolVariant3:
                    GlobalGame.clothMita = "FIIdClSchool";
                    GlobalGame.clothVariantMita = 2;
                    break;

                case Clothes.Chirstmass:
                    GlobalGame.clothMita = "Chirfns";
                    GlobalGame.clothVariantMita = 0;
                    break;

                case Clothes.Vampire:
                    GlobalGame.clothMita = "HellVamp";
                    GlobalGame.clothVariantMita = 0;
                    break;
            }

            currentClothes = cloth;
            UnityEngine.Object.FindObjectOfType<MitaClothes>().ReCloth();
        }
    }
}
