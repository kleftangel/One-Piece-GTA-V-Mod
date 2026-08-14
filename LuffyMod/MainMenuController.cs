using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using System;
using System.Windows.Forms;

namespace AnimeCharacterMod
{
    public class MainMenuController : Script
    {
        private ObjectPool pool = new ObjectPool();
        private NativeMenu mainMenu;

        private NativeCheckboxItem luffyToggle;
        private NativeCheckboxItem zoroToggle;
        private NativeCheckboxItem sanjiToggle;
        private NativeCheckboxItem chopperToggle;
        private NativeCheckboxItem frankyToggle;
        private NativeCheckboxItem aceToggle;
        private NativeCheckboxItem akainuToggle;
        private NativeCheckboxItem crocodileToggle;
        private NativeCheckboxItem oarsToggle;
        private NativeCheckboxItem aokijiToggle;
        private NativeCheckboxItem garpToggle;
        private NativeCheckboxItem kaidoToggle;
        private NativeCheckboxItem kidToggle;
        private NativeCheckboxItem arlongToggle;
        private NativeCheckboxItem lawToggle;
        private NativeCheckboxItem kuroToggle;
        private NativeCheckboxItem doflamingoToggle;


        private bool isMenuReady = false;
        private int startupDelayTime = 0;

        public static bool IsLuffyActive = false;
        public static bool IsZoroActive = false;
        public static bool IsSanjiActive = false;
        public static bool IsChopperActive = false;
        public static bool IsFrankyActive = false;
        public static bool IsAceActive = false;
        public static bool IsAkainuActive = false;
        public static bool IsCrocodileActive = false;
        public static bool IsOarsActive = false;
        public static bool IsAokijiActive = false;
        public static bool IsGarpActive = false;
        public static bool IsKaidoActive = false;
        public static bool IsKidActive = false;
        public static bool IsArlongActive = false;
        public static bool IsLawActive = false;
        public static bool IsKuroActive = false;
        public static bool IsDoflamingoActive = false;


        // --- ZORO CUSTOM COMPONENT VARIATION MATRIX ---
        // Format: { ComponentID, DrawableID, TextureID }
        private static readonly int[,] ZoroDefaultComponents = new int[,] {
            { 0, 1, 0 }, // Head 1, Texture 0
            { 8, 6, 0 }  // Accessory/Top 6, Texture 0
        };

        public MainMenuController()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void BuildInGameMenu()
        {
            mainMenu = new NativeMenu("Anime Core Menu", "SELECT ACTIVE ROSTER");

            luffyToggle = new NativeCheckboxItem("Luffy Active", "Enable controls & logic for Luffy.", false);
            zoroToggle = new NativeCheckboxItem("Zoro Active", "Enable controls & logic for Zoro.", false);
            sanjiToggle = new NativeCheckboxItem("Sanji Active", "Enable controls & logic for Sanji.", false);
            chopperToggle = new NativeCheckboxItem("Chopper Active", "Enable controls & logic for Chopper.", false);
            frankyToggle = new NativeCheckboxItem("Franky Active", "Enable controls & logic for Franky.", false);
            aceToggle = new NativeCheckboxItem("Ace Active", "Enable controls & logic for Ace.", false);
            akainuToggle = new NativeCheckboxItem("Akainu Active", "Enable controls & logic for Akainu.", false);
            crocodileToggle = new NativeCheckboxItem("Crocodile Active", "Enable controls & logic for Crocodile.", false);
            oarsToggle = new NativeCheckboxItem("Oars Active", "Enable controls & logic for Oars.", false);
            aokijiToggle = new NativeCheckboxItem("Aokiji Active", "Enable controls & logic for Aokiji.", false);
            garpToggle = new NativeCheckboxItem("Garp Active", "Enable controls & logic for Garp.", false);
            kaidoToggle = new NativeCheckboxItem("Kaido Active", "Enable controls & logic for Kaido.", false);
            kidToggle = new NativeCheckboxItem("Eustass Kid Active", "Enable controls & logic for Kid.", false);
            arlongToggle = new NativeCheckboxItem("Arlong Active", "Enable controls & logic for Arlong.", false);
            lawToggle = new NativeCheckboxItem("Law Active", "Enable controls & logic for Law.", false);
            kuroToggle = new NativeCheckboxItem("Kuro Active", "Enable controls & logic for Kuro.", false);
            doflamingoToggle = new NativeCheckboxItem("Doflamingo Active", "Enable controls & logic for Doflamingo.", false);

            mainMenu.Add(luffyToggle);
            mainMenu.Add(zoroToggle);
            mainMenu.Add(sanjiToggle);
            mainMenu.Add(chopperToggle);
            mainMenu.Add(frankyToggle);
            mainMenu.Add(aceToggle);
            mainMenu.Add(akainuToggle);
            mainMenu.Add(crocodileToggle);
            mainMenu.Add(oarsToggle);
            mainMenu.Add(aokijiToggle);
            mainMenu.Add(garpToggle);
            mainMenu.Add(kaidoToggle);
            mainMenu.Add(kidToggle);
            mainMenu.Add(arlongToggle);
            mainMenu.Add(lawToggle);
            mainMenu.Add(kuroToggle);
            mainMenu.Add(doflamingoToggle);

            pool.Add(mainMenu);

            luffyToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(1, luffyToggle.Checked);
            zoroToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(2, zoroToggle.Checked);
            sanjiToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(3, sanjiToggle.Checked);
            chopperToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(4, chopperToggle.Checked);
            frankyToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(5, frankyToggle.Checked);
            aceToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(6, aceToggle.Checked);
            akainuToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(7, akainuToggle.Checked);
            crocodileToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(8, crocodileToggle.Checked);
            oarsToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(9, oarsToggle.Checked);
            aokijiToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(10, aokijiToggle.Checked);
            garpToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(11, garpToggle.Checked);
            kaidoToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(12, kaidoToggle.Checked);
            kidToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(13, kidToggle.Checked);
            arlongToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(14, arlongToggle.Checked);
            lawToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(15, lawToggle.Checked);
            kuroToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(16, kuroToggle.Checked);
            doflamingoToggle.CheckboxChanged += (s, a) => EnforceMutualExclusion(17, doflamingoToggle.Checked);

            isMenuReady = true;
        }

        // Track how many frames the button has been held down
        private int selectButtonHoldDuration = 0;

        private void OnTick(object sender, EventArgs e)
        {
            if (!isMenuReady)
            {
                if (startupDelayTime == 0)
                {
                    startupDelayTime = Game.GameTime + 2000;
                }

                if (Game.GameTime < startupDelayTime) return;

                BuildInGameMenu();
                return;
            }

            pool.Process();

            if (!mainMenu.Visible)
            {
                // 0 = Primary Player Gamepad/Keyboard pad index
                // 0 = Control.NextCamera / View Button (Select / Back Button)
                bool holdingSelect = GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_DISABLED_CONTROL_PRESSED, 0, 0);

                if (holdingSelect)
                {
                    selectButtonHoldDuration++;

                    // Every frame tick is roughly 16ms. 90 frames = ~1.5 seconds of holding down
                    if (selectButtonHoldDuration >= 80)
                    {
                        mainMenu.Visible = true;
                        selectButtonHoldDuration = 0; // Reset tracking matrix
                        Script.Wait(400); // Prevent menu instantly flickering close
                    }
                }
                else
                {
                    selectButtonHoldDuration = 0; // Reset immediately if finger lifts off
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!isMenuReady) return;

            if (e.KeyCode == Keys.F11)
            {
                mainMenu.Visible = !mainMenu.Visible;
            }
        }

        // Exact string identifiers matching your AddonPeds configuration layout
        private const string LUFFY_PED_MODEL_NAME = "MonkeyDLuffy";
        private const string ZORO_PED_MODEL_NAME = "One_Piece_Zoro";
        private const string CHOPPER_PED_MODEL_NAME = "Chopper";
        private const string FRANKY_PED_MODEL_NAME = "Franky V2";
        private const string ACE_PED_MODEL_NAME = "AceJUMPFORCE";
        private const string AKAINU_PED_MODEL_NAME = "Akainu";
        private const string CROCODILE_PED_MODEL_NAME = "Crocodile";
        private const string OARS_PED_MODEL_NAME = "Oz";
        private const string AOKIJI_PED_MODEL_NAME = "Aokiji";
        private const string GARP_PED_MODEL_NAME = "Garp";
        private const string KAIDO_PED_MODEL_NAME = "Kaido";
        private const string KID_PED_MODEL_NAME = "Eustass Kid";
        private const string ARLONG_PED_MODEL_NAME = "Arlong";
        private const string LAW_PED_MODEL_NAME = "Law New World Cotume";
        private const string KURO_PED_MODEL_NAME = "Kuro";
        private const string DOFLAMINGO_PED_MODEL_NAME = "Doflamingo";


        // FIXED SIGNATURE: Added the 'bool state' parameter to match your checkbox event overloads perfectly
        private bool isUpdatingUI = false;

        private void EnforceMutualExclusion(int selectedID, bool state)
        {
            // 1. Exit if we are currently updating the UI to prevent loops
            if (isUpdatingUI) return;

            // 2. Unchecking the currently active item now deactivates the whole roster --
            //    no character stays selected until the player checks a new one.
            if (!state)
            {
                isUpdatingUI = true;

                IsLuffyActive = IsZoroActive = IsSanjiActive = IsChopperActive = IsFrankyActive = IsAceActive = IsAkainuActive = IsCrocodileActive = IsOarsActive = IsAokijiActive = IsGarpActive = IsKaidoActive = IsKidActive = IsArlongActive = IsLawActive = IsKuroActive = IsDoflamingoActive = false;
                luffyToggle.Checked = zoroToggle.Checked = sanjiToggle.Checked = chopperToggle.Checked = frankyToggle.Checked = aceToggle.Checked = akainuToggle.Checked = crocodileToggle.Checked = oarsToggle.Checked = aokijiToggle.Checked = garpToggle.Checked = kaidoToggle.Checked = kidToggle.Checked = arlongToggle.Checked = lawToggle.Checked = kuroToggle.Checked = doflamingoToggle.Checked = false;

                isUpdatingUI = false;
                return;
            }

            // 3. Engage Lock & Update UI/Logic
            isUpdatingUI = true;

            // Reset all
            IsLuffyActive = IsZoroActive = IsSanjiActive = IsChopperActive = IsFrankyActive = IsAceActive = IsAkainuActive = IsCrocodileActive = IsOarsActive = IsAokijiActive = IsGarpActive = IsKaidoActive = IsKidActive = IsArlongActive = IsLawActive = IsKuroActive = IsDoflamingoActive = false;
            luffyToggle.Checked = zoroToggle.Checked = sanjiToggle.Checked = chopperToggle.Checked = frankyToggle.Checked = aceToggle.Checked = akainuToggle.Checked = crocodileToggle.Checked = oarsToggle.Checked = aokijiToggle.Checked = garpToggle.Checked = kaidoToggle.Checked = kidToggle.Checked = arlongToggle.Checked = lawToggle.Checked = kuroToggle.Checked = doflamingoToggle.Checked = false;

            // Set selected
            if (selectedID == 1)
            {
                IsLuffyActive = true;
                luffyToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(LUFFY_PED_MODEL_NAME, null); // FIXED
            }
            else if (selectedID == 2)
            {
                IsZoroActive = true;
                zoroToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(ZORO_PED_MODEL_NAME, ZoroDefaultComponents); // FIXED
            }
            else if (selectedID == 3)
            {
                IsSanjiActive = true;
                sanjiToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(null, null); // FIXED
            }
            else if (selectedID == 4)
            {
                IsChopperActive = true;
                chopperToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(CHOPPER_PED_MODEL_NAME, null); // FIXED
            }
            else if (selectedID == 5)
            {
                IsFrankyActive = true;
                frankyToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(FRANKY_PED_MODEL_NAME, null); // FIXED
            }
            else if (selectedID == 6)
            {
                IsAceActive = true;
                aceToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(ACE_PED_MODEL_NAME, null); // FIXED
            }
            else if (selectedID == 7)
            {
                IsAkainuActive = true;
                akainuToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(AKAINU_PED_MODEL_NAME, null);
            }
            else if (selectedID == 8)
            {
                IsCrocodileActive = true;
                crocodileToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(CROCODILE_PED_MODEL_NAME, null);
            }
            else if (selectedID == 9)
            {
                IsOarsActive = true;
                oarsToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(OARS_PED_MODEL_NAME, null);
            }
            else if (selectedID == 10)
            {
                IsAokijiActive = true;
                aokijiToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(AOKIJI_PED_MODEL_NAME, null);
            }
            else if (selectedID == 11)
            {
                IsGarpActive = true;
                garpToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(GARP_PED_MODEL_NAME, null);
            }
            else if (selectedID == 12)
            {
                IsKaidoActive = true;
                kaidoToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(KAIDO_PED_MODEL_NAME, null);
            }
            else if (selectedID == 13)
            {
                IsKidActive = true;
                kidToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(KID_PED_MODEL_NAME, null);
            }
            else if (selectedID == 14)
            {
                IsArlongActive = true;
                arlongToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(ARLONG_PED_MODEL_NAME, null);
            }
            else if (selectedID == 15)
            {
                IsLawActive = true;
                lawToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(LAW_PED_MODEL_NAME, null);
            }
            else if (selectedID == 16)
            {
                IsKuroActive = true;
                kuroToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(KURO_PED_MODEL_NAME, null);
            }
            else if (selectedID == 17)
            {
                IsDoflamingoActive = true;
                doflamingoToggle.Checked = true; /* Load Model */
                ChangePlayerPedModel(DOFLAMINGO_PED_MODEL_NAME, null);
            }

            // 4. Release Lock
            isUpdatingUI = false;
        }

        public static void ChangePlayerPedModel(string modelName, int[,] componentMatrix = null)
        {
            // Sanji protection: If the model name is null or skipped, exit immediately
            if (string.IsNullOrEmpty(modelName)) return;

            // 1. Convert the model name string directly into its numeric engine hash
            int modelHash = Function.Call<int>(Hash.GET_HASH_KEY, modelName);

            // 2. Safety check: Verify the custom model is registered inside the game asset system
            if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, modelHash)) return;

            // 3. Request the asset to stream into game RAM memory buffers
            Function.Call(Hash.REQUEST_MODEL, modelHash);

            // 4. Asynchronous Loading Loop: Wait until the model registers as fully loaded
            int timeoutTracker = 0;
            while (!Function.Call<bool>(Hash.HAS_MODEL_LOADED, modelHash) && timeoutTracker < 1000)
            {
                Script.Wait(10);
                timeoutTracker++;
            }

            // 5. Instantly swap the player's core model to your custom character model
            Function.Call(Hash.SET_PLAYER_MODEL, Function.Call<int>(Hash.PLAYER_ID), modelHash);

            // 6. Refresh textures to guarantee your One Piece skin clothes render cleanly
            int playerPedId = Function.Call<int>(Hash.PLAYER_PED_ID);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, playerPedId);

            // --- APPLY CUSTOM COMPONENTS ---
            if (componentMatrix != null)
            {
                for (int i = 0; i < componentMatrix.GetLength(0); i++)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, playerPedId,
                        componentMatrix[i, 0], componentMatrix[i, 1], componentMatrix[i, 2], 2);
                }
            }

            // 7. FIXED NATIVE CALL: Prefixed with SET_ to completely remove the CS0117 context warning
            Function.Call(Hash.SET_MODEL_AS_NO_LONGER_NEEDED, modelHash);
        }
    }
}