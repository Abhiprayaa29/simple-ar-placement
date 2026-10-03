using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleARPlacement
{
    /// <summary>
    /// Menangani UI: baris status AR, instruksi pengguna, pesan singkat (toast),
    /// serta tombol rotasi, skala, reset, dan tampilan plane.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARUIController : MonoBehaviour
    {
        [Header("Teks Status dan Instruksi")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI toastText;

        [Header("Tombol")]
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;
        [SerializeField] private Button scaleDownButton;
        [SerializeField] private Button scaleUpButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button planeToggleButton;

        [Header("Referensi Komponen")]
        [SerializeField] private ARPlacementManager placementManager;
        [SerializeField] private ARPlaneController planeController;

        [Header("Pengaturan")]
        [SerializeField] private float refreshInterval = 0.2f;
        [SerializeField] private float toastDuration = 2.5f;

        private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(16);

        private PointerEventData pointerEventData;
        private EventSystem lastEventSystem;
        private float nextRefreshTime;
        private float toastHideTime;
        private string lastStatus;
        private string lastInstruction;

        private void OnEnable()
        {
            AddListener(rotateLeftButton, OnRotateLeft);
            AddListener(rotateRightButton, OnRotateRight);
            AddListener(scaleDownButton, OnScaleDown);
            AddListener(scaleUpButton, OnScaleUp);
            AddListener(resetButton, OnReset);
            AddListener(planeToggleButton, OnTogglePlanes);
        }

        private void OnDisable()
        {
            RemoveListener(rotateLeftButton, OnRotateLeft);
            RemoveListener(rotateRightButton, OnRotateRight);
            RemoveListener(scaleDownButton, OnScaleDown);
            RemoveListener(scaleUpButton, OnScaleUp);
            RemoveListener(resetButton, OnReset);
            RemoveListener(planeToggleButton, OnTogglePlanes);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private void Start()
        {
            if (toastText != null)
            {
                toastText.gameObject.SetActive(false);
            }

            RefreshTexts();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + refreshInterval;
                RefreshTexts();
            }

            if (toastText != null && toastText.gameObject.activeSelf &&
                Time.unscaledTime >= toastHideTime)
            {
                toastText.gameObject.SetActive(false);
            }
        }

        private void RefreshTexts()
        {
            if (statusText != null)
            {
                string status = planeController != null
                    ? planeController.DescribeSession()
                    : "Status AR tidak tersedia";

                if (status != lastStatus)
                {
                    statusText.text = status;
                    lastStatus = status;
                }
            }

            if (instructionText != null)
            {
                bool placed = placementManager != null && placementManager.HasObject;
                string instruction = planeController != null
                    ? planeController.DescribeInstruction(placed)
                    : "Ketuk permukaan untuk menempatkan objek.";

                if (instruction != lastInstruction)
                {
                    instructionText.text = instruction;
                    lastInstruction = instruction;
                }
            }
        }

        /// <summary>Menampilkan pesan singkat yang hilang otomatis.</summary>
        public void ShowToast(string message)
        {
            if (toastText == null)
            {
                return;
            }

            toastText.text = message;
            toastText.gameObject.SetActive(true);
            toastHideTime = Time.unscaledTime + toastDuration;
        }

        /// <summary>True bila posisi sentuh berada di atas elemen UI (tombol/panel).</summary>
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (pointerEventData == null || lastEventSystem != eventSystem)
            {
                pointerEventData = new PointerEventData(eventSystem);
                lastEventSystem = eventSystem;
            }

            pointerEventData.position = screenPosition;
            RaycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, RaycastResults);
            return RaycastResults.Count > 0;
        }

        // ------------------------------------------------------------------
        // Penangan tombol
        // ------------------------------------------------------------------

        private void OnRotateLeft()
        {
            placementManager?.RotateLeft();
        }

        private void OnRotateRight()
        {
            placementManager?.RotateRight();
        }

        private void OnScaleUp()
        {
            placementManager?.ScaleUp();
        }

        private void OnScaleDown()
        {
            placementManager?.ScaleDown();
        }

        private void OnReset()
        {
            placementManager?.ResetPlacedObject();
        }

        private void OnTogglePlanes()
        {
            if (planeController == null)
            {
                return;
            }

            planeController.TogglePlaneVisualization();
            ShowToast(planeController.PlanesVisible
                ? "Visualisasi permukaan: AKTIF"
                : "Visualisasi permukaan: NONAKTIF");
        }
    }
}
