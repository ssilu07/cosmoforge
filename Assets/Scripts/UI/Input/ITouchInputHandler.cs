// ============================================================================
// CosmoForge: Touch Input Handler & Gesture Type Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.UI.Input
{
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    public enum TouchGestureType
    {
        None,
        SingleTap,
        DoubleTap,
        Pan,
        PinchZoom,
        Twist
    }

    public interface ITouchInputHandler
    {
        TouchGestureType LastGesture { get; }
        Vector3 CameraPosition { get; set; }
        float CameraDistance { get; set; }
        float CameraYaw { get; set; }
        int FocusedBodyId { get; set; }
        bool IsTrackingFocus { get; set; }

        void ProcessPan(Vector2 deltaScreen, float sensitivity);
        void ProcessPinch(float initialDistance, float currentDistance, float minZoom, float maxZoom);
        void ProcessTwist(float angleDeltaDegrees);
        int ProcessSingleTap(Vector3 tapWorldPos, IList<CelestialBodyState> bodies, double maxSelectionDistance);
        void ProcessDoubleTap(int bodyId);
    }
}
