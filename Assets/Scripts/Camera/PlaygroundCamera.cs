using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class PlaygroundCamera : MonoBehaviour
{
    public static float HudScale => Mathf.Min(Screen.width / 960f, Screen.height / 640f);

    [SerializeField] private Transform target;
    [SerializeField, Range(30f, 65f)] private float fieldOfView = 50f;
    [SerializeField, Min(1f)] private float followSpeed = 8f;
    [SerializeField, Range(40f, 80f)] private float tilt = 55f;
    [SerializeField, Range(18f, 38f)] private float distance = 25.5f;
    [SerializeField] private bool thirdPerson = true;
    [SerializeField, Range(10f, 40f)] private float thirdPersonTilt = 28f;
    [SerializeField, Range(3f, 14f)] private float thirdPersonDistance = 7f;
    public bool ThirdPerson { get => thirdPerson; set => thirdPerson = value; }
    public float MinTilt => thirdPerson ? 10f : 40f;
    public float MaxTilt => thirdPerson ? 40f : 80f;
    public float MinDistance => thirdPerson ? 3f : 18f;
    public float MaxDistance => thirdPerson ? 14f : 38f;
    public float Tilt { get => thirdPerson ? thirdPersonTilt : tilt; set { if(thirdPerson) thirdPersonTilt=Mathf.Clamp(value,10f,40f); else tilt=Mathf.Clamp(value,40f,80f); } }
    public float Distance { get => thirdPerson ? thirdPersonDistance : distance; set { if(thirdPerson) thirdPersonDistance=Mathf.Clamp(value,3f,14f); else distance=Mathf.Clamp(value,18f,38f); } }
    [SerializeField, Range(30f, 240f)] private float yawSpeed = 140f;
    public float Yaw { get; private set; }
    [SerializeField, Range(.02f,1f)] private float mouseSensitivity=.18f;
    private InputAction orbitAction;
    private PlayerController player;
    private float displayedDistance;
    private float distanceVelocity;
    private Vector3 collisionOffset;
    private PlayerAvatar avatar;
    private float lastObstructedAt;
    private Camera arenaCamera;
    private Vector3 focus;
    private float shakeUntil,shakeStrength,shakeDuration;
    public void Shake(float strength,float duration)
    {
        if(Time.time<shakeUntil)
        {
            float currentRemaining=shakeStrength*Mathf.Clamp01((shakeUntil-Time.time)/Mathf.Max(.01f,shakeDuration));
            if(strength<=currentRemaining) return;
        }
        shakeStrength=strength; shakeDuration=duration; shakeUntil=Time.time+duration;
    }
    public void StopShake() => shakeUntil=0;

    private void Awake()
    {
        LoadPreferences();
        orbitAction = new InputAction("Camera Orbit", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");
        arenaCamera = GetComponent<Camera>();
        displayedDistance = Distance;
        arenaCamera.orthographic = false;
        arenaCamera.fieldOfView = fieldOfView;
    }

    public void LoadPreferences()
    {
        thirdPerson=PlayerPrefs.GetInt("Camera.ThirdPerson",thirdPerson?1:0)!=0;
        thirdPersonTilt=Mathf.Clamp(PlayerPrefs.GetFloat("Camera.ThirdTilt",thirdPersonTilt),10,40);
        thirdPersonDistance=Mathf.Clamp(PlayerPrefs.GetFloat("Camera.ThirdDistance",thirdPersonDistance),3,14);
        tilt=Mathf.Clamp(PlayerPrefs.GetFloat("Camera.TopTilt",tilt),40,80);
        distance=Mathf.Clamp(PlayerPrefs.GetFloat("Camera.TopDistance",distance),18,38);
    }
    public void SavePreferences()
    {
        PlayerPrefs.SetInt("Camera.ThirdPerson",thirdPerson?1:0);
        PlayerPrefs.SetFloat("Camera.ThirdTilt",thirdPersonTilt);
        PlayerPrefs.SetFloat("Camera.ThirdDistance",thirdPersonDistance);
        PlayerPrefs.SetFloat("Camera.TopTilt",tilt);
        PlayerPrefs.SetFloat("Camera.TopDistance",distance);
        PlayerPrefs.Save();
    }
    public void ResetCurrentView()
    {
        Tilt=thirdPerson?28f:55f; Distance=thirdPerson?7f:25.5f;
    }

    private void OnEnable() => orbitAction.Enable();
    private void OnDisable() { orbitAction.Disable(); if(avatar!=null) avatar.SetCameraOpacity(1f); foreach(var obstacle in CameraObstacleBounds.Active) obstacle.Restore(); }
    private void OnDestroy() => orbitAction.Dispose();

    private void Start()
    {
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
        player = target != null ? target.GetComponent<PlayerController>() : null;
        avatar = target != null ? target.GetComponent<PlayerAvatar>() : null;
        focus = target != null ? new Vector3(target.position.x, 0f, target.position.z) : Vector3.zero;
        ApplyView();
    }

    private void LateUpdate()
    {
        if (thirdPerson && player != null && player.enabled && Time.timeScale>0 && Application.isFocused)
        {
            ApplyLook(orbitAction.ReadValue<Vector2>().x,
                Mouse.current!=null?Mouse.current.delta.ReadValue().x:0,
                Mouse.current!=null && Mouse.current.rightButton.isPressed,Time.deltaTime);
        }
        // Compact HUD cards overlay the corners; the world uses the full screen.
        arenaCamera.rect = new Rect(0f, 0f, 1f, 1f);
        if (target != null)
        {
            Vector3 desiredFocus = new Vector3(target.position.x, 0f, target.position.z);
            focus = Vector3.Lerp(focus, desiredFocus, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }
        ApplyView();
    }

    internal void ApplyLook(float stick,float mouseDelta,bool dragging,float dt)
    {
        if(!thirdPerson || player==null || !player.enabled || Time.timeScale<=0) return;
        // Mouse delta is already a per-frame displacement, unlike stick velocity.
        Yaw=Mathf.Repeat(Yaw+stick*yawSpeed*dt+(dragging?mouseDelta*mouseSensitivity:0),360f);
    }

    private void ApplyView()
    {
        Vector3 lookAt = focus + (thirdPerson ? Vector3.up * 1.35f : Vector3.zero);
        Quaternion rotation = Quaternion.Euler(Tilt, thirdPerson ? Yaw : 0f, 0f);
        Vector3 backward = -(rotation * Vector3.forward);
        displayedDistance=Distance;
        Vector3 desired=lookAt+backward*Distance;
        // Only the camera's own footprint can move it, never a blocked sight line.
        collisionOffset=thirdPerson?Vector3.Lerp(collisionOffset,Vector3.zero,1-Mathf.Exp(-8f*Time.deltaTime)):Vector3.zero;
        Vector3 position=thirdPerson?ProtectCamera(desired+collisionOffset):desired;
        collisionOffset=position-desired;
        transform.SetPositionAndRotation(position,rotation);
        foreach(var obstacle in CameraObstacleBounds.Active)
        {
            Bounds bounds=obstacle.VisualBounds; bounds.Expand(.15f);
            Vector3 toCamera=position-lookAt;
            bool obscures=thirdPerson && bounds.IntersectRay(new Ray(lookAt,toCamera.normalized),out float at)
                && at<toCamera.magnitude;
            if(!thirdPerson) obstacle.Restore();
            else obstacle.SetObscuring(obscures,Time.deltaTime);
        }
        if(Time.time<shakeUntil)
        {
            float strength=shakeStrength*Mathf.Clamp01((shakeUntil-Time.time)/Mathf.Max(.01f,shakeDuration));
            Vector3 shakeOffset=transform.TransformDirection(new Vector3(Mathf.Sin(Time.time*85f)*strength*.35f,Mathf.Cos(Time.time*95f)*strength*.25f,0));
            transform.position=position+shakeOffset;
            transform.rotation*=Quaternion.Euler(Mathf.Sin(Time.time*95f)*strength*4f,Mathf.Cos(Time.time*80f)*strength*2f,Mathf.Cos(Time.time*110f)*strength*3f);
        }
        if(avatar!=null)
        {
            float opacity=thirdPerson?Mathf.SmoothStep(0,1,
                Mathf.InverseLerp(.1f,.9f,avatar.DistanceToVisual(transform.position))):1f;
            // Fade out immediately if squeezed inside the model; ease its reappearance.
            avatar.SetCameraOpacity(!thirdPerson?1f:opacity<avatar.CameraOpacity?opacity:
                Mathf.MoveTowards(avatar.CameraOpacity,opacity,Time.deltaTime*5f));
        }
        arenaCamera.fieldOfView = thirdPerson ? 60f : fieldOfView;
    }
    private Vector3 ProtectCamera(Vector3 position)
    {
        float halfHeight=arenaCamera.nearClipPlane*Mathf.Tan(30f*Mathf.Deg2Rad);
        float radius=Mathf.Max(.2f,halfHeight*Mathf.Sqrt(1+arenaCamera.aspect*arenaCamera.aspect)+.05f);
        for(int pass=0;pass<4;pass++)
        {
            foreach(var obstacle in CameraObstacleBounds.Active)
                position=OutsideBounds(position,obstacle.VisualBounds,radius);
            foreach(Collider collider in Physics.OverlapSphere(position,radius,~0,QueryTriggerInteraction.Ignore))
            {
                if(collider.GetComponent<CameraObstacleBounds>()!=null) continue;
                if(target!=null && (collider.transform==target || collider.transform.IsChildOf(target))) continue;
                position=OutsideBounds(position,collider.bounds,radius);
            }
        }
        return position;
    }
    private static Vector3 OutsideBounds(Vector3 point,Bounds bounds,float radius)
    {
        bounds.Expand(radius*2);
        if(!bounds.Contains(point)) return point;
        float best=float.PositiveInfinity; Vector3 offset=Vector3.zero;
        for(int axis=0;axis<3;axis++)
        {
            float low=point[axis]-bounds.min[axis], high=bounds.max[axis]-point[axis];
            float distance=Mathf.Min(low,high);
            if(distance>=best) continue;
            best=distance; offset=Vector3.zero; offset[axis]=low<high?-low-.01f:high+.01f;
        }
        return point+offset;
    }
}
