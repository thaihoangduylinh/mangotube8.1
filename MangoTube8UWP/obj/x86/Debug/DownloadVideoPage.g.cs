﻿

#pragma checksum "D:\Git\mangotube81\MangoTube8UWP\DownloadVideoPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "8003CA03AF91C2844BF7FCC90F40C582"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MangoTube8UWP
{
    partial class VideoDownloadPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 83 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.Media.Animation.Timeline)(target)).Completed += this.HideSearchBox_Completed;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 337 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Pivot)(target)).SelectionChanged += this.MainPivot_SelectionChanged;
                 #line default
                 #line hidden
                #line 337 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).Loaded += this.MainPivot_Loaded;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 308 "..\..\..\DownloadVideoPage.xaml"
                ((global::Microsoft.PlayerFramework.MediaPlayer)(target)).MediaEnded += this.AudioPlayer_MediaEnded;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 326 "..\..\..\DownloadVideoPage.xaml"
                ((global::Microsoft.PlayerFramework.MediaPlayer)(target)).VolumeChanged += this.VideoPlayer_VolumeChanged;
                 #line default
                 #line hidden
                #line 327 "..\..\..\DownloadVideoPage.xaml"
                ((global::Microsoft.PlayerFramework.MediaPlayer)(target)).MediaOpened += this.VideoPlayer_MediaOpened;
                 #line default
                 #line hidden
                #line 328 "..\..\..\DownloadVideoPage.xaml"
                ((global::Microsoft.PlayerFramework.MediaPlayer)(target)).IsFullScreenChanged += this.VideoPlayer_IsFullScreenChanged;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 279 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Downloads_Tapped;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 285 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.History_Tapped;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 208 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.AccountButton_Click;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 232 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.YouTubeLogo_Tapped;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 260 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.SearchButton_Click;
                 #line default
                 #line hidden
                break;
            case 10:
                #line 246 "..\..\..\DownloadVideoPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).KeyDown += this.SearchTextBox_KeyDown;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


