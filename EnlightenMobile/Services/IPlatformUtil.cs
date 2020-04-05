using System;
namespace EnlightenMobile.Services
{
    // @todo split into PlatformUIService, PlatformFileService etc
    public interface IPlatformUtil
    {
        void toast(string msg);
        string getSavePath();
    }
}
