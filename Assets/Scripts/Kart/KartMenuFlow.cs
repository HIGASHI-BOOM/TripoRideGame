using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum KartMenuInitialPage
{
    MainMenu,
    TireSelect
}

public sealed class KartMenuFlow : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private KartController playerKart;
    [SerializeField] private KartWheelSet wheelSet;
    [SerializeField] private KartHud hud;

    [Header("UI References")]
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject tireSelectPage;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button startRaceButton;
    [SerializeField] private Button smallTireButton;
    [SerializeField] private Button mediumTireButton;
    [SerializeField] private Button largeTireButton;
    [SerializeField] private Text selectedTireText;

    [Header("Scene Flow")]
    [SerializeField] private KartMenuInitialPage initialPage = KartMenuInitialPage.MainMenu;
    [SerializeField] private bool startGameLoadsConfigScene = true;
    [SerializeField] private bool startRaceLoadsRaceScene = true;
    [SerializeField] private string vehicleConfigSceneName = "KartVehicleConfig";
    [SerializeField] private string raceSceneName = "KartFormalGame";

    [Header("State")]
    [SerializeField] private KartTireSize selectedTireSize = KartTireSize.Medium;

    public KartTireSize SelectedTireSize => selectedTireSize;

    private void Awake()
    {
        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }

        if (wheelSet == null)
        {
            wheelSet = FindAnyObjectByType<KartWheelSet>();
        }

        if (hud == null)
        {
            hud = FindAnyObjectByType<KartHud>();
        }

        selectedTireSize = KartGameState.SelectedTireSize;
        RegisterButtonEvents();

        if (initialPage == KartMenuInitialPage.TireSelect)
        {
            ShowTireSelect();
        }
        else
        {
            ShowMainMenu();
        }
    }

    public void StartGame()
    {
        KartGameState.SelectedTireSize = selectedTireSize;

        if (startGameLoadsConfigScene && !string.IsNullOrWhiteSpace(vehicleConfigSceneName))
        {
            SceneManager.LoadScene(vehicleConfigSceneName);
            return;
        }

        ShowTireSelect();
    }

    public void ShowMainMenu()
    {
        SetPage(mainPage, true);
        SetPage(tireSelectPage, false);

        if (hud != null)
        {
            hud.enabled = false;
        }

        if (wheelSet != null)
        {
            wheelSet.ApplyWheelSize(selectedTireSize);
        }

        if (playerKart != null)
        {
            playerKart.SetInputEnabled(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshTireButtons();
    }

    public void ShowTireSelect()
    {
        SetPage(mainPage, false);
        SetPage(tireSelectPage, true);
        PrepareMenuState();
        RefreshTireButtons();
    }

    public void SelectTireSize(KartTireSize size)
    {
        selectedTireSize = size;
        KartGameState.SelectedTireSize = selectedTireSize;
        wheelSet?.ApplyWheelSize(size);
        RefreshTireButtons();
    }

    public void StartRace()
    {
        KartGameState.SelectedTireSize = selectedTireSize;
        SetPage(mainPage, false);
        SetPage(tireSelectPage, false);
        wheelSet?.ApplyWheelSize(selectedTireSize);

        if (startRaceLoadsRaceScene && !string.IsNullOrWhiteSpace(raceSceneName))
        {
            SceneManager.LoadScene(raceSceneName);
            return;
        }

        if (hud != null)
        {
            hud.enabled = true;
        }

        if (playerKart != null)
        {
            playerKart.SetInputEnabled(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameObject.SetActive(false);
    }

    private void RegisterButtonEvents()
    {
        startGameButton?.onClick.AddListener(StartGame);
        startRaceButton?.onClick.AddListener(StartRace);
        smallTireButton?.onClick.AddListener(() => SelectTireSize(KartTireSize.Small));
        mediumTireButton?.onClick.AddListener(() => SelectTireSize(KartTireSize.Medium));
        largeTireButton?.onClick.AddListener(() => SelectTireSize(KartTireSize.Large));
    }

    private void PrepareMenuState()
    {
        if (hud != null)
        {
            hud.enabled = false;
        }

        if (wheelSet != null)
        {
            wheelSet.ApplyWheelSize(selectedTireSize);
        }

        if (playerKart != null)
        {
            playerKart.SetInputEnabled(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RefreshTireButtons()
    {
        SetButtonInteractable(smallTireButton, selectedTireSize != KartTireSize.Small);
        SetButtonInteractable(mediumTireButton, selectedTireSize != KartTireSize.Medium);
        SetButtonInteractable(largeTireButton, selectedTireSize != KartTireSize.Large);

        if (selectedTireText != null)
        {
            selectedTireText.text = $"Selected: {selectedTireSize}";
        }
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void SetPage(GameObject page, bool active)
    {
        if (page != null)
        {
            page.SetActive(active);
        }
    }
}
