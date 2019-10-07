using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public PlayerData PlayerData;
    public Image EnergyBar;
    public List<TrailRenderer> Trails;
    private FSM<PlayerController> _playerFSM;
    private Camera _camera;
    private Rigidbody _rb;
    private float _curEnergy;
    private float _boosterForce;
    private Transform _meshPivot;

    private void Awake()
    {
        _playerFSM = new FSM<PlayerController>(this);
        _camera = Camera.main;
        _rb = GetComponent<Rigidbody>();
        _curEnergy = PlayerData.MaxEnergy;
        _meshPivot = transform.Find("MeshPivot");
        _playerFSM.TransitionTo<PlayerOnGroundIdleState>();
    }

    // Update is called once per frame
    void Update()
    {
        _playerFSM.Update();
    }

    private void FixedUpdate()
    {
        _playerFSM.FixedUpdate();
    }

    private void LateUpdate()
    {
        _playerFSM.LateUpdate();
    }

    private void _updateEnergy(float amt)
    {
        _curEnergy += amt;
        if (_curEnergy <= 0f)
        {
            _curEnergy = 0f;
        }
        else if (_curEnergy >= PlayerData.MaxEnergy)
        {
            _curEnergy = PlayerData.MaxEnergy;
        }
        EnergyBar.fillAmount = _curEnergy / PlayerData.MaxEnergy;
    }

    private abstract class PlayerState : FSM<PlayerController>.State
    {
        protected float _HLAxis { get { return Input.GetAxis("Horizontal"); } }
        protected float _VLAxis { get { return Input.GetAxis("Vertical"); } }
        protected bool _JumpUp { get { return Input.GetKeyUp(KeyCode.Space); } }
        protected bool _JumpDown { get { return Input.GetKeyDown(KeyCode.Space); } }
        protected bool _Jump { get { return Input.GetKey(KeyCode.Space); } }
        protected float _MouseX { get { return Input.GetAxis("Mouse X"); } }
        protected float _MouseY { get { return Input.GetAxis("Mouse Y"); } }
        protected bool _MouseLeft { get { return Input.GetMouseButtonDown(0); } }
        protected PlayerData _PlayerData;

        public override void Init()
        {
            base.Init();
            _PlayerData = Context.PlayerData;
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        protected bool _isOnGround()
        {
            // RaycastHit hit;
            return Physics.Raycast(Context.transform.position, Vector3.down, _PlayerData.GroundCastLength, _PlayerData.GroundLayer);
        }
    }

    private abstract class PlayerOnGroundState : PlayerState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Context._boosterForce = 0f;
        }
        public override void Update()
        {
            base.Update();
            if (_JumpUp && _isOnGround())
            {
                Context._rb.AddForce(Vector3.up * (_PlayerData.JumpInitialSpeed + Context._boosterForce), ForceMode.VelocityChange);
                TransitionTo<PlayerFreeFallState>();
                return;
            }
            if (!_Jump && _isOnGround())
            {
                Context._updateEnergy(Time.deltaTime * _PlayerData.OnGroundEnergyRecover);
            }
            else if (_Jump)
            {
                Context._updateEnergy(Time.deltaTime * _PlayerData.BoosterSpeedEnergyUseage);
                Context._boosterForce += Time.deltaTime * _PlayerData.BoosterSpeedIncrementalSpeed;
                if (Context._boosterForce >= _PlayerData.MaxBoosterSpeed ||
                    Context._curEnergy <= 0f)
                {
                    Context._rb.AddForce(Vector3.up * (_PlayerData.JumpInitialSpeed + Context._boosterForce), ForceMode.VelocityChange);
                    TransitionTo<PlayerFreeFallState>();
                    return;
                }
            }
            else if (!_isOnGround())
            {
                TransitionTo<PlayerFreeFallState>();
                return;
            }
        }
    }

    private abstract class PlayerInAirState : PlayerState
    {
        public override void LateUpdate()
        {
            base.LateUpdate();
            if (_isOnGround() && Context._rb.velocity.y < -0.02f)
            {
                TransitionTo<PlayerOnGroundIdleState>();
                return;
            }
        }
    }

    private class PlayerGlideState : PlayerInAirState
    {
        private Vector3 _dir;
        public override void OnEnter()
        {
            base.OnEnter();
            Vector3 yVel = Context._rb.velocity;
            yVel.y = -1f;
            Context._rb.velocity = yVel;
            foreach (var trail in Context.Trails)
            {
                trail.enabled = true;
            }
        }

        float ClampAngle(float angle, float from, float to)
        {
            // accepts e.g. -80, 80
            if (angle < 0f) angle = 360 + angle;
            if (angle > 180f) return Mathf.Max(angle, 360 + from);
            return Mathf.Min(angle, to);
        }

        public override void Update()
        {
            base.Update();
            Context.transform.Rotate(Time.deltaTime * _PlayerData.GlideTurnSpeed * _HLAxis * Vector3.up);
            if (Input.GetAxisRaw("Horizontal") != 0f)
                Context._meshPivot.Rotate(Time.deltaTime * _PlayerData.MaxGlideZRotation * -_HLAxis * Vector3.forward);
            else
            {
                float rot = (Context._meshPivot.localEulerAngles.z > 180) ? Context._meshPivot.localEulerAngles.z - 360 : Context._meshPivot.localEulerAngles.z;
                rot = rot == 0f ? 0f : (rot > 0f ? -1f : 1f);
                Context._meshPivot.Rotate(Time.deltaTime * _PlayerData.GlideZRotationRecoverSpeed * rot * Vector3.forward);
            }
            Vector3 _rot = Context._meshPivot.localEulerAngles;
            _rot.z = ClampAngle(_rot.z, -_PlayerData.MaxGlideZRotation, _PlayerData.MaxGlideZRotation);
            Context._meshPivot.localEulerAngles = _rot;

            Context._updateEnergy(_PlayerData.GlideEnergyUsage * Time.deltaTime);
            if (Context._curEnergy <= 0f)
            {
                TransitionTo<PlayerFreeFallState>();
                return;
            }
            if (_JumpDown)
            {
                TransitionTo<PlayerFreeFallState>();
                return;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            Context._rb.AddForce(_PlayerData.GlideUpForce * Vector3.up, ForceMode.Acceleration);

            Vector3 targetVelocity = Context.transform.forward * _PlayerData.GlideSpeed;
            Vector3 velocityChange = targetVelocity - Context._rb.velocity;
            velocityChange.y = 0f;

            Context._rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        public override void OnExit()
        {
            base.OnExit();
            foreach (var trail in Context.Trails)
            {
                trail.enabled = false;
            }
            Context._meshPivot.localEulerAngles = Vector3.zero;
        }
    }

    private class PlayerFreeFallState : PlayerInAirState
    {
        private Vector3 _dir;
        public override void Update()
        {
            base.Update();
            if (_JumpDown && Context._curEnergy > 0f)
            {
                TransitionTo<PlayerGlideState>();
                return;
            }
            if (Context._rb.velocity.y > 0f &&
            Context._rb.velocity.y < 1f &&
            Context._boosterForce > _PlayerData.EnableGlideModeBoosterForceThreshold &&
            Context._curEnergy > 0f)
            {
                TransitionTo<PlayerGlideState>();
                return;
            }

            if (Input.GetAxisRaw("Horizontal") == 0f && Input.GetAxisRaw("Vertical") == 0f) return;
            _dir = new Vector3(_HLAxis, 0f, _VLAxis).normalized;
            _dir = Quaternion.AngleAxis(Context._camera.transform.eulerAngles.y, Vector3.up) * _dir;
            _dir.Normalize();
            float angle = Mathf.Atan2(_dir.x, _dir.z) * Mathf.Rad2Deg;
            Vector3 tempEuler = Context.transform.localEulerAngles;
            tempEuler.y = angle;
            Context.transform.localEulerAngles = tempEuler;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (Input.GetAxisRaw("Horizontal") == 0f && Input.GetAxisRaw("Vertical") == 0f) return;
            Vector3 targetVelocity = _dir * _PlayerData.WalkSpeed;
            Vector3 velocityChange = targetVelocity - Context._rb.velocity;
            velocityChange.y = 0f;

            Context._rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    private class PlayerOnGroundIdleState : PlayerOnGroundState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Context._rb.velocity = Vector3.zero;
        }

        public override void Update()
        {
            base.Update();
            if (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f)
            {
                TransitionTo<PlayerOnGroundWalkState>();
                return;
            }

        }
    }

    private class PlayerOnGroundWalkState : PlayerOnGroundState
    {
        private Vector3 _dir;

        public override void Update()
        {
            base.Update();
            _dir = new Vector3(_HLAxis, 0f, _VLAxis).normalized;
            _dir = Quaternion.AngleAxis(Context._camera.transform.eulerAngles.y, Vector3.up) * _dir;
            _dir.Normalize();
            float angle = Mathf.Atan2(_dir.x, _dir.z) * Mathf.Rad2Deg;
            Vector3 tempEuler = Context.transform.localEulerAngles;
            tempEuler.y = angle;
            Context.transform.localEulerAngles = tempEuler;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            Vector3 targetVelocity = _dir * _PlayerData.WalkSpeed;
            Vector3 velocityChange = targetVelocity - Context._rb.velocity;
            velocityChange.y = 0f;

            Context._rb.AddForce(velocityChange, ForceMode.VelocityChange);
            if (Input.GetAxisRaw("Horizontal") == 0f && Input.GetAxisRaw("Vertical") == 0f)
            {
                TransitionTo<PlayerOnGroundIdleState>();
                return;
            }
        }
    }
}
