using UnityEngine;

namespace Blackzone.Combat
{
    /// <summary>
    /// Marks a hit region on a target (head vs body). Placed on a small child
    /// collider ("Head") of enemy/player rigs; Ballistics checks it for the
    /// headshot multiplier.
    /// </summary>
    public sealed class HitRegion : MonoBehaviour
    {
        [SerializeField] private bool isHead;

        public bool IsHead => isHead;

        public void Configure(bool head)
        {
            isHead = head;
        }
    }
}
