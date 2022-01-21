using UnityEngine;
using System.Collections;

namespace JamKit
{
    public class Footsteps : MonoBehaviour
    {
        [SerializeField] // Play a footstep after this much of travel
        private float _stepDistance = 2f;

        [SerializeField] // Play a the heavy fall SFX if falling down faster than this
        private float _heavyFallSfxVelocityLimit = 25f;

        private Vector3 _prevVelocity;
        private float _footstepDistance;

        public void Tick(bool isGrounded, Vector3 velocity, float travelThisFrame)
        {
            if (!isGrounded)
            {
                _footstepDistance = 0;
                return;
            }

            // At the moment of stopping:
            // - force play the sfx, it makes it sound more natural, humans usually stop with an extra step
            // - reset the distance, to make the footsteps ryhtym the same at start of walking every time
            if (_prevVelocity.ToHorizontal().magnitude > 0.01 && velocity.ToHorizontal().magnitude < 0.01)
            {
                _footstepDistance = 0f;
                Sfx.Instance.PlayRandom("Footstep");
            }

            _footstepDistance += travelThisFrame;

            if (_footstepDistance > _stepDistance)
            {
                _footstepDistance = 0f;
                Sfx.Instance.PlayRandom("Footstep");
            }

            _prevVelocity = velocity;
        }

        public void HandleLandSfx(Vector3 collisionDisplacement, Vector3 velocityBeforeCrop)
        {
            // TODO @BUG: This doesn't play consistently. Figure out why
            if (collisionDisplacement.magnitude < 0.008f // There needs to be some penetration
                || Vector3.Dot(collisionDisplacement.normalized, Gravity.Down) > -0.9f) // That displacement is against gravity
            {
                return;
            }

            float verticalVelocity = Vector3.Dot(velocityBeforeCrop, Gravity.Down);

            if (verticalVelocity > _heavyFallSfxVelocityLimit)
            {
                Sfx.Instance.Play("LandFromHeight");
            }
            else
            {
                Sfx.Instance.Play("Land");
            }

        }
    }
}