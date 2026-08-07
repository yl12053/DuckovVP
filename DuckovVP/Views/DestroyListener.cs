using System;
using UnityEngine;

namespace DuckovVP.Views;

public class DestroyListener: MonoBehaviour
{
    public Action Destroy;

    private void OnDestroy()
    {
        Destroy?.Invoke();
    }
}