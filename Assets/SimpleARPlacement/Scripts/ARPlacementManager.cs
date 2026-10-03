using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SimpleARPlacement
{
    /// <summary>
    /// Menangani input tap pengguna, raycast ke permukaan AR, serta penempatan,
    /// pemindahan, rotasi, skala, dan penghapusan objek 3D.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARPlacementManager : MonoBehaviour
    {
        [Header("Aset")]
        [Tooltip("Prefab objek yang akan ditempatkan. Jika kosong, kubus cadangan dibuat saat runtime.")]
        [SerializeField] private GameObject placeablePrefab;

        [Tooltip("Indikator visual (ring/pin) yang mengikuti hasil raycast sebelum objek ditempatkan.")]
        [SerializeField] private Transform placementIndicator;

        [Header("Referensi Komponen")]
        [SerializeField] private ARPlaneController planeController;
        [SerializeField] private ARUIController uiController;

        [Header("Pengaturan Interaksi")]
        [SerializeField] private float rotateStepDegrees = 30f;
        [SerializeField] private float scaleStep = 1.2f;
        [SerializeField] private float minScale = 0.4f;
        [SerializeField] private float maxScale = 3f;
        [SerializeField] private float placeCooldown = 0.25f;
        [SerializeField] private TrackableType raycastMask = TrackableType.PlaneWithinPolygon;

        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>(8);

        private ARRaycastManager raycastManager;
        private ARPlaneManager planeManager;
        private ARAnchorManager anchorManager;

        private GameObject placedObject;
        private ARAnchor placedAnchor;
        private Quaternion basePoseRotation = Quaternion.identity;
        private float yawOffset;
        private float scale = 1f;
        private float nextPlaceTime;

        public bool HasObject => placedObject != null;
        public int PlaneCount => planeManager != null ? planeManager.trackables.count : 0;
        public float CurrentScale => scale;
        public GameObject PlacedObject => placedObject;

        private void Awake()
        {
            raycastManager = GetComponent<ARRaycastManager>();
            planeManager = GetComponent<ARPlaneManager>();
            anchorManager = GetComponent<ARAnchorManager>();

            if (raycastManager == null)
            {
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }

            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }

            if (anchorManager == null)
            {
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
            }

            if (raycastManager == null)
            {
                Debug.LogError(
                    "[SimpleARPlacement] ARRaycastManager tidak ditemukan. Tap-to-place tidak akan berfungsi.");
            }

            if (placeablePrefab == null)
            {
                Debug.LogWarning(
                    "[SimpleARPlacement] Prefab objek belum diisi. Akan dipakai kubus cadangan saat runtime.");
            }

            SetIndicatorActive(false);
        }

        private void Update()
        {
            UpdateIndicator();

            if (TryGetPointerDown(out Vector2 screenPosition))
            {
                HandleTap(screenPosition);
            }
        }

        /// <summary>Membaca posisi pointer (jari/mouse) yang baru ditekan.</summary>
        private bool TryGetPointerDown(out Vector2 screenPosition)
        {
            screenPosition = default;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            return false;
        }

        /// <summary>Posisi raycast: jari yang sedang menyentuh layar, atau mouse, atau tengah layar.</summary>
        private bool TryGetRaycastPosition(out Vector2 screenPosition)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.isInProgress)
            {
                screenPosition = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            // Di perangkat mobile tanpa sentuhan aktif, gunakan tengah layar agar
            // indikator mengikuti arah pandang kamera.
            if (Application.isMobilePlatform)
            {
                screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return true;
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (Time.unscaledTime < nextPlaceTime)
            {
                return;
            }

            if (uiController != null && uiController.IsPointerOverUI(screenPosition))
            {
                return;
            }

            if (raycastManager == null || !raycastManager.Raycast(screenPosition, hits, raycastMask) ||
                hits.Count == 0)
            {
                uiController?.ShowToast("Tidak ada permukaan pada titik tersebut. Arahkan kamera ke permukaan.");
                return;
            }

            PlaceObject(hits[0]);
            nextPlaceTime = Time.unscaledTime + placeCooldown;
        }

        private void PlaceObject(ARRaycastHit hit)
        {
            if (placedObject == null)
            {
                GameObject prefab = placeablePrefab;
                if (prefab == null)
                {
                    prefab = BuildFallbackObject();
                }

                placedObject = Instantiate(prefab);
                placedObject.name = "Placed Object";
                yawOffset = 0f;
                scale = 1f;
                uiController?.ShowToast("Objek ditempatkan.");
            }
            else
            {
                uiController?.ShowToast("Objek dipindahkan.");
            }

            ApplyPoseToPlacedObject(hit);
        }

        /// <summary>
        /// Menempatkan/memindahkan objek tepat pada pose hasil raycast.
        /// Jika memungkinkan, objek diikat ke ARAnchor agar menempel pada permukaan
        /// dan tetap mengikuti perbaikan tracking.
        /// </summary>
        private void ApplyPoseToPlacedObject(ARRaycastHit hit)
        {
            Pose pose = hit.pose;
            basePoseRotation = pose.rotation;

            DetachAnchor();

            ARPlane plane = planeManager != null ? planeManager.GetPlane(hit.trackableId) : null;
            ARAnchor anchor = null;

            if (anchorManager != null && anchorManager.enabled && plane != null)
            {
                try
                {
                    anchor = anchorManager.AttachAnchor(plane, pose);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SimpleARPlacement] Gagal membuat anchor: {e.Message}. " +
                                     "Objek akan ditempatkan langsung pada pose raycast.");
                }
            }

            if (anchor != null)
            {
                placedAnchor = anchor;
                placedObject.transform.SetParent(anchor.transform, false);
                placedObject.transform.localPosition = Vector3.zero;
                placedObject.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);
            }
            else
            {
                placedObject.transform.SetParent(null, true);
                placedObject.transform.SetPositionAndRotation(
                    pose.position,
                    pose.rotation * Quaternion.Euler(0f, yawOffset, 0f));
            }

            placedObject.transform.localScale = Vector3.one * scale;
        }

        private void DetachAnchor()
        {
            if (placedAnchor == null)
            {
                return;
            }

            if (placedObject != null)
            {
                placedObject.transform.SetParent(null, true);
            }

            if (anchorManager != null)
            {
                try
                {
                    anchorManager.TryRemoveAnchor(placedAnchor);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SimpleARPlacement] Gagal menghapus anchor: {e.Message}");
                }
            }

            placedAnchor = null;
        }

        private void UpdateIndicator()
        {
            if (placementIndicator == null || raycastManager == null)
            {
                return;
            }

            if (!TryGetRaycastPosition(out Vector2 screenPosition) ||
                !raycastManager.Raycast(screenPosition, hits, raycastMask) || hits.Count == 0)
            {
                SetIndicatorActive(false);
                return;
            }

            Pose pose = hits[0].pose;
            placementIndicator.SetPositionAndRotation(
                pose.position,
                pose.rotation * Quaternion.Euler(0f, yawOffset, 0f));

            if (!placementIndicator.gameObject.activeSelf)
            {
                SetIndicatorActive(true);
            }
        }

        private void SetIndicatorActive(bool active)
        {
            if (placementIndicator != null && placementIndicator.gameObject.activeSelf != active)
            {
                placementIndicator.gameObject.SetActive(active);
            }
        }

        // ------------------------------------------------------------------
        // API untuk UI
        // ------------------------------------------------------------------

        public void RotateLeft()
        {
            Rotate(-rotateStepDegrees);
        }

        public void RotateRight()
        {
            Rotate(rotateStepDegrees);
        }

        private void Rotate(float degrees)
        {
            if (placedObject == null)
            {
                uiController?.ShowToast("Belum ada objek. Ketuk permukaan untuk menempatkan objek.");
                return;
            }

            yawOffset += degrees;

            if (placedAnchor != null)
            {
                placedObject.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);
            }
            else
            {
                placedObject.transform.rotation =
                    basePoseRotation * Quaternion.Euler(0f, yawOffset, 0f);
            }
        }

        public void ScaleUp()
        {
            ApplyScale(scale * scaleStep);
        }

        public void ScaleDown()
        {
            ApplyScale(scale / scaleStep);
        }

        private void ApplyScale(float newScale)
        {
            if (placedObject == null)
            {
                uiController?.ShowToast("Belum ada objek. Ketuk permukaan untuk menempatkan objek.");
                return;
            }

            scale = Mathf.Clamp(newScale, minScale, maxScale);
            placedObject.transform.localScale = Vector3.one * scale;
        }

        public void ResetPlacedObject()
        {
            if (placedObject == null)
            {
                uiController?.ShowToast("Belum ada objek untuk direset.");
                return;
            }

            DetachAnchor();
            Destroy(placedObject);
            placedObject = null;
            yawOffset = 0f;
            scale = 1f;
            uiController?.ShowToast("Objek dihapus. Ketuk permukaan untuk menempatkan lagi.");
        }

        /// <summary>Menghitung jumlah permukaan yang terdeteksi (untuk UI).</summary>
        public string DescribePlanes()
        {
            int count = PlaneCount;
            if (count == 0)
            {
                return "belum ada";
            }

            return count + " permukaan terdeteksi";
        }

        /// <summary>
        /// Membuat objek cadangan berupa kubus jika prefab tidak tersedia,
        /// sehingga fitur penempatan tetap dapat diverifikasi.
        /// </summary>
        private GameObject BuildFallbackObject()
        {
            Debug.LogError("[SimpleARPlacement] Prefab objek tidak tersedia. " +
                           "Membuat kubus cadangan pada runtime sebagai pengganti.");

            GameObject root = new GameObject("Fallback Cube");
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            cube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    Material material = new Material(shader) { color = new Color(0.9f, 0.45f, 0.1f) };
                    renderer.material = material;
                }
            }

            return root;
        }
    }
}
