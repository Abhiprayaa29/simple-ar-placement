using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace SimpleARPlacement
{
    /// <summary>
    /// Mengelola deteksi permukaan (plane), visualisasi plane, izin kamera,
    /// serta status tracking AR untuk ditampilkan ke UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARPlaneController : MonoBehaviour
    {
        [Header("Referensi Komponen")]
        [Tooltip("Jika kosong, diambil dari komponen pada GameObject yang sama / di scene.")]
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Pengaturan Deteksi")]
        [Tooltip("Batasi deteksi hanya pada permukaan horizontal (lantai/meja).")]
        [SerializeField] private bool horizontalOnly = true;

        [Tooltip("Tampilkan visualisasi plane saat aplikasi dimulai.")]
        [SerializeField] private bool showVisualizationOnStart = true;

        [Tooltip("Minta izin kamera saat aplikasi dimulai (Android).")]
        [SerializeField] private bool requestCameraPermissionOnStart = true;

        [Tooltip("Waktu tunggu (detik) sebelum izin kamera dianggap ditolak.")]
        [SerializeField] private float permissionTimeout = 6f;

        private bool visualizationVisible = true;
        private bool permissionFlowActive;
        private float permissionRequestedAt;

        public bool CameraPermissionGranted { get; private set; } = true;
        public bool PlanesVisible => visualizationVisible;
        public int PlaneCount => planeManager != null ? planeManager.trackables.count : 0;
        public bool HasPlanes => PlaneCount > 0;
        public ARSessionState SessionState => ARSession.state;
        public bool IsSessionTracking => ARSession.state == ARSessionState.SessionTracking;

        /// <summary>Alasan kamera/AR belum tracking (None berarti tracking berjalan).</summary>
        public string NotTrackingReason => ARSession.notTrackingReason.ToString();

        /// <summary>True bila permintaan izin kamera sudah melewati batas waktu tanpa jawaban.</summary>
        public bool CameraPermissionDenied =>
            permissionFlowActive && !CameraPermissionGranted &&
            Time.unscaledTime - permissionRequestedAt > permissionTimeout;

        /// <summary>True bila izin kamera sedang menunggu jawaban pengguna.</summary>
        public bool CameraPermissionPending =>
            permissionFlowActive && !CameraPermissionGranted && !CameraPermissionDenied;

        private void Awake()
        {
            if (planeManager == null)
            {
                planeManager = GetComponent<ARPlaneManager>();
            }

            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }

            if (planeManager == null)
            {
                Debug.LogError("[SimpleARPlacement] ARPlaneManager tidak ditemukan. " +
                               "Deteksi permukaan tidak akan berfungsi.");
            }
        }

        private void OnEnable()
        {
            ARSession.stateChanged += OnSessionStateChanged;

            if (planeManager != null)
            {
                planeManager.trackablesChanged.AddListener(OnPlanesChanged);
            }
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= OnSessionStateChanged;

            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
            }
        }

        private void Start()
        {
            ApplyDetectionMode();
            SetPlaneVisualization(showVisualizationOnStart);

            if (requestCameraPermissionOnStart)
            {
                RequestCameraPermission();
            }
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (permissionFlowActive && !CameraPermissionGranted &&
                Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                CameraPermissionGranted = true;
                Debug.Log("[SimpleARPlacement] Izin kamera diberikan.");
            }
#endif
        }

        private void RequestCameraPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                CameraPermissionGranted = true;
                permissionFlowActive = false;
                return;
            }

            CameraPermissionGranted = false;
            permissionFlowActive = true;
            permissionRequestedAt = Time.unscaledTime;
            Permission.RequestUserPermission(Permission.Camera);
            Debug.Log("[SimpleARPlacement] Meminta izin kamera...");
#else
            CameraPermissionGranted = true;
            permissionFlowActive = false;
#endif
        }

        private void ApplyDetectionMode()
        {
            if (planeManager == null)
            {
                return;
            }

            planeManager.requestedDetectionMode = horizontalOnly
                ? PlaneDetectionMode.Horizontal
                : PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
        }

        private void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            Debug.Log($"[SimpleARPlacement] Status ARSession: {args.state}");

            if (args.state == ARSessionState.Unsupported)
            {
                Debug.LogError("[SimpleARPlacement] Perangkat ini tidak mendukung ARCore.");
            }
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (!visualizationVisible)
            {
                SetPlaneVisualization(false);
            }
        }

        /// <summary>Menampilkan/menyembunyikan visualisasi plane tanpa menghentikan deteksi.</summary>
        public void SetPlaneVisualization(bool visible)
        {
            visualizationVisible = visible;

            if (planeManager == null)
            {
                return;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane == null)
                {
                    continue;
                }

                Renderer[] renderers = plane.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        public void TogglePlaneVisualization()
        {
            SetPlaneVisualization(!visualizationVisible);
        }

        /// <summary>Ringkasan status AR untuk baris status UI.</summary>
        public string DescribeSession()
        {
            string permission = string.Empty;
            if (CameraPermissionDenied)
            {
                permission = " | Izin kamera: DITOLAK";
            }
            else if (CameraPermissionPending)
            {
                permission = " | Izin kamera: menunggu";
            }

            return $"AR: {SessionState}{permission} | Plane: {PlaneCount}";
        }

        /// <summary>Pesan instruksi utama untuk pengguna.</summary>
        public string DescribeInstruction(bool objectPlaced)
        {
            if (CameraPermissionDenied)
            {
                return "Izin kamera ditolak. Buka Pengaturan > Aplikasi > Izin > Kamera, lalu coba lagi.";
            }

            if (SessionState == ARSessionState.Unsupported)
            {
                return "Perangkat ini tidak mendukung ARCore. Aplikasi tidak dapat memulai AR.";
            }

            if (SessionState == ARSessionState.CheckingAvailability ||
                SessionState == ARSessionState.NeedsInstall ||
                SessionState == ARSessionState.Installing)
            {
                return "Menyiapkan AR pada perangkat, mohon tunggu...";
            }

            if (SessionState != ARSessionState.SessionTracking)
            {
                if (CameraPermissionPending)
                {
                    return "Menunggu izin kamera agar AR dapat dimulai...";
                }

                return "Mengaktifkan kamera AR. Arahkan perlahan ke lantai atau meja.";
            }

            if (NotTrackingReason != "None")
            {
                return $"Tracking terganggu ({NotTrackingReason}). Arahkan kamera ke permukaan bertekstur dan gerakkan perlahan.";
            }

            if (!HasPlanes)
            {
                return "Arahkan kamera ke lantai atau meja, lalu gerakkan perlahan.";
            }

            return objectPlaced
                ? "Ketuk permukaan lain untuk memindahkan objek."
                : "Ketuk permukaan untuk menempatkan objek.";
        }
    }
}
