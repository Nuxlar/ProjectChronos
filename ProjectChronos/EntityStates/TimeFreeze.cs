using RoR2;
using EntityStates;
using UnityEngine;

namespace ProjectChronos.EntityStates
{
    public class TimeFreeze : BaseState
    {
        private Animator modelAnimator;
        public float freezeDuration = 5f;

        public override void OnEnter()
        {
            base.OnEnter();
            // this.characterBody.AddTimedBuff(ChronoItems.timeFrozenBuff, freezeDuration);
            if ((bool)this.sfxLocator && this.sfxLocator.barkSound != "")
            {
                Util.PlaySound(this.sfxLocator.barkSound, this.gameObject);
            }
            this.modelAnimator = this.GetModelAnimator();
            if ((bool)this.modelAnimator)
            {
                this.modelAnimator.enabled = false;
            }
            if ((bool)this.rigidbody && !this.rigidbody.isKinematic)
            {
                this.rigidbody.velocity = Vector3.zero;
                if ((bool)this.rigidbodyMotor)
                    this.rigidbodyMotor.moveVector = Vector3.zero;
            }
            // this.healthComponent.isInFrozenState = true;
            if (!(bool)this.characterDirection)
                return;
            this.characterDirection.moveVector = this.characterDirection.forward;
        }

        public override void OnExit()
        {
            if ((bool)this.modelAnimator)
                this.modelAnimator.enabled = true;
            // this.healthComponent.isInFrozenState = false;
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!this.isAuthority || this.characterBody.HasBuff(ProjectChronos.timeFrozenBuff))
                return;
            this.outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}
