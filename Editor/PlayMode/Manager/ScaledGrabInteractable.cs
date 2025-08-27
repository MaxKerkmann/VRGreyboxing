using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRGreyboxing;

namespace VRGreyboxing
{
    public class ScaledGrabInteractable : XRGrabInteractable
    {
    // One attach Transform per interactor (keyed by the interactor's Transform to avoid interface identity issues)
    private readonly Dictionary<Transform, Transform> _attachPerInteractor = new();

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);

        var interactorTf = args.interactorObject?.transform;
        if (interactorTf == null) return;

        // Create (or reuse) a dynamic attach for this interactor
        if (!_attachPerInteractor.TryGetValue(interactorTf, out var attach) || attach == null)
        {
            attach = new GameObject($"{name}_Attach_{interactorTf.name}").transform;
            attach.SetParent(transform, false);
            attach.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _attachPerInteractor[interactorTf] = attach;
        }

        // Position attach at the interactor's grip pose in WORLD space
        var src = args.interactorObject.GetAttachTransform(this);
        var pos = src ? src.position : interactorTf.position;
        var rot = src ? src.rotation : interactorTf.rotation;

        if (IsFinite(pos) && IsFinite(rot))
            attach.SetPositionAndRotation(pos, rot);
        else
            attach.SetPositionAndRotation(transform.position, transform.rotation);
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);

        var interactorTf = args.interactorObject?.transform;
        if (interactorTf && _attachPerInteractor.TryGetValue(interactorTf, out var attach))
        {
            if (attach) Destroy(attach.gameObject);
            _attachPerInteractor.Remove(interactorTf);
        }
    }

    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        var tf = interactor?.transform;
        if (tf && _attachPerInteractor.TryGetValue(tf, out var attach) && attach)
            return attach;

        return base.GetAttachTransform(interactor);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        foreach (var kv in _attachPerInteractor)
            if (kv.Value) Destroy(kv.Value.gameObject);
        _attachPerInteractor.Clear();
    }

    private static bool IsFinite(Vector3 v) =>
        !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
          float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

    private static bool IsFinite(Quaternion q) =>
        !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w) ||
          float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w));
    }
}