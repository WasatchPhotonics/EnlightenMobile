using Xamarin.Forms;

namespace EnlightenMobile.Services
{
    // @todo split into PlatformUIService, PlatformFileService etc
    public interface IPlatformUtil
    {
        void toast(string msg, View view = null);
        string getSavePath();
    }
}
