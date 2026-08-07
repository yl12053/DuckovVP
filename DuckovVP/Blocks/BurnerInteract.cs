using DuckovVP.Views;

namespace DuckovVP.Blocks;

public class BurnerInteract: InteractableBase
{
    protected override void OnInteractFinished()
    {
        if (ViewUtils.burnerView != null)
        {
            ViewUtils.burnerView.gameObject.SetActive(true);
            ViewUtils.burnerView.Open(null);
        }
    }
}