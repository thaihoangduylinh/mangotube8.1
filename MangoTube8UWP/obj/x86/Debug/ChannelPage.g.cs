﻿

#pragma checksum "D:\Git\mangotube81\MangoTube8UWP\ChannelPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "7B886E52BCDD77D56214E7A68823D3CA"
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
    partial class ChannelPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 65 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.Media.Animation.Timeline)(target)).Completed += this.HideSearchBox_Completed;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 310 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Pivot)(target)).SelectionChanged += this.MainPivot_SelectionChanged;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 343 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.LoadMoreButton_Click;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 229 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Downloads_Tapped;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 235 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.History_Tapped;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 158 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.AccountButton_Click;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 182 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.YouTubeLogo_Tapped;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 210 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.SearchButton_Click;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 196 "..\..\..\ChannelPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).KeyDown += this.SearchTextBox_KeyDown;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


