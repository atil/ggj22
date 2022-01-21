using UnityEngine;
using System.Collections;

namespace JamKit
{
    public class PrintTrigger : TriggerBase
    {
        protected override bool TriggerOnce => false;

        // Example trigger usage
        protected override void OnTriggered()
        {
            Debug.Log("triggered!");
        }
    }
}