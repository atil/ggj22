using UnityEngine;
using System.Collections;

namespace JamKit
{
    [RequireComponent(typeof(BoxCollider))]
    public abstract class TriggerBase : MonoBehaviour
    {
        protected abstract bool TriggerOnce { get; }

        private bool _isTriggered = false;

        void OnTriggerEnter(Collider collider)
        {
            if (TriggerOnce && _isTriggered)
            {
                return;
            }
            
            if (collider.gameObject.layer != 1 << LayerMask.NameToLayer("Trigger"))
            {
                Debug.Log("Other trigger");
                return;
            }

            _isTriggered = true;
            OnTriggered();
        }

        protected abstract void OnTriggered();

        public void ResetTrigger()
        {
            _isTriggered = false;
        }
    }
}