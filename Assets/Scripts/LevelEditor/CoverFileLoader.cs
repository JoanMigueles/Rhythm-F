using SFB;
using UnityEngine;

public class CoverFileLoader : MonoBehaviour
{
    public void OpenCoverFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpeg", "jpg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Cover File", "", extensions, false);

    }
}
