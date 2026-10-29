using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public sealed partial class PlayerController
{
    public const float DashDistance=2.5f,DashDuration=.15f,DashCooldown=4.0f;
    public const int MaxDashCharges=2;
    private const float ArenaPlayerLimit=17.4f;
    private InputAction dashAction;
    private PlayerHealth health;
    private bool dashHeld,focusLost;
    private Vector3 lastMoveDirection=Vector3.forward,dashDirection;
    private float dashRemaining,dashCooldownRemaining,dashReadyFlash;
    private int dashCharges=MaxDashCharges;
    private float footstepDistance;
    private int dashProtectionFrame=-1;
    private Gamepad rumblingPad;
    private float rumbleUntil;
    private TrailRenderer[] dashTrails;
    private Material dashTrailMaterial;
    public bool IsDashing=>isActiveAndEnabled && dashRemaining>0;
    public bool IsDashProtected=>isActiveAndEnabled && !focusLost && Time.timeScale>0
        && (dashRemaining>0 || dashProtectionFrame==Time.frameCount);
    public float DashCooldownRemaining=>dashCooldownRemaining;
    public float DashReadyFlash=>dashReadyFlash;
    public int DashCharges=>dashCharges;
    public int MaxCharges=>MaxDashCharges;

    private void InitializeDash()
    {
        health=GetComponent<PlayerHealth>();
        dashAction=new InputAction("Short dash",InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/leftShoulder");
        dashAction.wantsInitialStateCheck=true;
        InputSystem.onDeviceChange+=DashDeviceChanged;
    }
    private void PollMovement(float dt)
    {
        // Observe releases even while the codex has frozen game time. Never queue menu presses.
        bool held=dashAction.IsPressed();
        bool pressed=held && !dashHeld; dashHeld=held;
        if(rumblingPad!=null && Time.unscaledTime>=rumbleUntil) StopDashRumble();
        if(focusLost || Time.timeScale<=0) {SuspendDashFeedback(); return;}
        TickMovement(ReadMoveDirection(),pressed,dt);
    }
    private void TickMovement(Vector3 movement,bool dashPressed,float dt)
    {
        if(!isActiveAndEnabled || !characterController.enabled || focusLost || Time.timeScale<=0
            || (health!=null && health.IsDead) || float.IsNaN(dt) || float.IsInfinity(dt) || dt<=0) return;
        Vector3 travelStart=transform.position;
        movement.y=0;
        movement=Vector3.ClampMagnitude(movement,1);
        dashProtectionFrame=-1;
        if(dashRemaining<=0 && movement.sqrMagnitude>.001f) lastMoveDirection=movement.normalized;
        if(dashPressed && dashCharges>0 && dashRemaining<=0)
        {
            dashCharges--;
            dashDirection=movement.sqrMagnitude>.001f?movement.normalized:lastMoveDirection;
            dashRemaining=DashDuration;
            if(dashCooldownRemaining<=0) dashCooldownRemaining=DashCooldown;
            dashReadyFlash=0;
            GameAudio.Play(AudioCue.Dash); footstepDistance=0;
            EnsureDashTrails();
            foreach(var trail in dashTrails) {trail.Clear(); trail.emitting=true;}
            // Only rumble the controller that supplied the dash input.
            rumblingPad=dashAction.activeControl?.device as Gamepad;
            if(rumblingPad!=null) {rumblingPad.SetMotorSpeeds(.12f,.22f); rumbleUntil=Time.unscaledTime+.08f;}
        }
        if(dashCharges<MaxDashCharges)
        {
            float before=dashCooldownRemaining;
            dashCooldownRemaining=Mathf.Max(0,dashCooldownRemaining-dt);
            if(before>0 && dashCooldownRemaining==0)
            {
                dashCharges++;
                dashReadyFlash=.6f;
                if(dashCharges<MaxDashCharges) dashCooldownRemaining=DashCooldown;
            }
        }
        else
        {
            dashCooldownRemaining=0;
        }
        dashReadyFlash=Mathf.Max(0,dashReadyFlash-dt);
        bool dashTravel=dashRemaining>0;
        if(dashRemaining>0)
        {
            float step=DashDistance/DashDuration*Mathf.Min(dt,dashRemaining);
            Vector3 origin=transform.position,target=origin+dashDirection*step;
            target.x=Mathf.Clamp(target.x,-ArenaPlayerLimit,ArenaPlayerLimit);
            target.z=Mathf.Clamp(target.z,-ArenaPlayerLimit,ArenaPlayerLimit);
            bool border=(target-origin).sqrMagnitude+0.0001f<step*step;
            // CharacterController performs a swept move against cover and solid stone enemies.
            var collision=characterController.Move(target-origin);
            dashRemaining=Mathf.Max(0,dashRemaining-dt);
            if(dashRemaining<.00001f) dashRemaining=0;
            if(border || (collision&CollisionFlags.Sides)!=0) dashRemaining=0;
            else dashProtectionFrame=Time.frameCount; // Protect the final movement frame too.
            avatar?.Face(dashDirection);
        }
        else
        {
            avatar?.Face(movement);
            characterController.Move(movement*(moveSpeed*SpeedMultiplier*dt));
        }
        Vector3 traveled=transform.position-travelStart; traveled.y=0;
        if(traveled.sqrMagnitude>.000001f) LastTravelDirection=traveled.normalized;
        if(!dashTravel && traveled.sqrMagnitude>.000001f)
        {
            footstepDistance+=traveled.magnitude;
            if(footstepDistance>=1.65f) { footstepDistance%=1.65f; GameAudio.Play(AudioCue.Footstep); }
        }
        else footstepDistance=0;
        bool active=dashRemaining>0;
        avatar?.SetDashing(active);
        if(dashTrails!=null) foreach(var trail in dashTrails) trail.emitting=active;
    }
    public void ResetDash()
    {
        footstepDistance=0;
        dashCharges=MaxDashCharges;
        dashRemaining=0; dashCooldownRemaining=0; dashReadyFlash=0; dashProtectionFrame=-1;
        LastTravelDirection=Vector3.forward; lastMoveDirection=Vector3.forward; dashDirection=Vector3.zero; dashHeld=true;
        SuspendDashFeedback();
    }
    private void SuspendDashFeedback()
    {
        StopDashRumble(); avatar?.SetDashing(false);
        if(dashTrails!=null) foreach(var trail in dashTrails) {trail.emitting=false; trail.Clear();}
    }
    private void StopDashRumble()
    {
        if(rumblingPad!=null && rumblingPad.added) rumblingPad.SetMotorSpeeds(0,0);
        rumblingPad=null;
    }
    private void OnApplicationFocus(bool focused)
    {
        focusLost=!focused; dashHeld=true;
        if(!focused) SuspendDashFeedback();
    }
    private void DashDeviceChanged(InputDevice device,InputDeviceChange change)
    {
        if(device==rumblingPad && (change==InputDeviceChange.Disconnected || change==InputDeviceChange.Removed || change==InputDeviceChange.Disabled)) StopDashRumble();
    }
    private void EnsureDashTrails()
    {
        if(dashTrails!=null) return;
        dashTrailMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")) {name="Player Dash Streak",color=new Color(.55f,.92f,1,.65f)};
        dashTrailMaterial.SetFloat("_Surface",1);
        dashTrailMaterial.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
        dashTrailMaterial.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        dashTrailMaterial.SetInt("_ZWrite",0);
        dashTrailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); dashTrailMaterial.renderQueue=(int)RenderQueue.Transparent;
        dashTrails=new TrailRenderer[2];
        for(int i=0;i<dashTrails.Length;i++)
        {
            var go=new GameObject("Dash Streak"); go.transform.SetParent(transform,false);
            go.transform.localPosition=new Vector3(i==0?-.2f:.2f,.12f,0);
            var trail=go.AddComponent<TrailRenderer>(); trail.sharedMaterial=dashTrailMaterial;
            trail.time=.12f; trail.minVertexDistance=.035f; trail.startWidth=.18f; trail.endWidth=0;
            trail.shadowCastingMode=ShadowCastingMode.Off; trail.receiveShadows=false; trail.emitting=false;
            dashTrails[i]=trail;
        }
    }
    private void DisposeDash()
    {
        InputSystem.onDeviceChange-=DashDeviceChanged;
        StopDashRumble(); dashAction?.Dispose();
        if(dashTrailMaterial!=null) Destroy(dashTrailMaterial);
    }
}
