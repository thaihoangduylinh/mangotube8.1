﻿

#pragma checksum "D:\Git\mangotube81\MangoTube8UWP\SearchPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "3AE34E024CB9040AB41F719F71456F8B"
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
    partial class SearchPage : global::Windows.UI.Xaml.Controls.Page
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Media.Animation.Storyboard ShowSearchBox; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Media.Animation.Storyboard HideSearchBox; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Media.Animation.Storyboard BubbleLoadingAnimation; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Media.Animation.Storyboard ShowDropDown; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Media.Animation.Storyboard HideDropDown; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Canvas DropDown; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Grid ThisLandIsOurLand; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.StackPanel LoadingPanel; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.ItemsControl SearchItemsControl; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Button LoadMore; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Shapes.Ellipse Bubble1; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Shapes.Ellipse Bubble2; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Shapes.Ellipse Bubble3; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Button AccountButton; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Image YouTubeLogo; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Grid SearchContainer; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Button SearchButton; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Image SearchIcon; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.TextBox SearchTextBox; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private global::Windows.UI.Xaml.Controls.Image AccountIcon; 
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        private bool _contentLoaded;

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {
            if (_contentLoaded)
                return;

            _contentLoaded = true;
            global::Windows.UI.Xaml.Application.LoadComponent(this, new global::System.Uri("ms-appx:///SearchPage.xaml"), global::Windows.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Application);
 
            ShowSearchBox = (global::Windows.UI.Xaml.Media.Animation.Storyboard)this.FindName("ShowSearchBox");
            HideSearchBox = (global::Windows.UI.Xaml.Media.Animation.Storyboard)this.FindName("HideSearchBox");
            BubbleLoadingAnimation = (global::Windows.UI.Xaml.Media.Animation.Storyboard)this.FindName("BubbleLoadingAnimation");
            ShowDropDown = (global::Windows.UI.Xaml.Media.Animation.Storyboard)this.FindName("ShowDropDown");
            HideDropDown = (global::Windows.UI.Xaml.Media.Animation.Storyboard)this.FindName("HideDropDown");
            DropDown = (global::Windows.UI.Xaml.Controls.Canvas)this.FindName("DropDown");
            ThisLandIsOurLand = (global::Windows.UI.Xaml.Controls.Grid)this.FindName("ThisLandIsOurLand");
            LoadingPanel = (global::Windows.UI.Xaml.Controls.StackPanel)this.FindName("LoadingPanel");
            SearchItemsControl = (global::Windows.UI.Xaml.Controls.ItemsControl)this.FindName("SearchItemsControl");
            LoadMore = (global::Windows.UI.Xaml.Controls.Button)this.FindName("LoadMore");
            Bubble1 = (global::Windows.UI.Xaml.Shapes.Ellipse)this.FindName("Bubble1");
            Bubble2 = (global::Windows.UI.Xaml.Shapes.Ellipse)this.FindName("Bubble2");
            Bubble3 = (global::Windows.UI.Xaml.Shapes.Ellipse)this.FindName("Bubble3");
            AccountButton = (global::Windows.UI.Xaml.Controls.Button)this.FindName("AccountButton");
            YouTubeLogo = (global::Windows.UI.Xaml.Controls.Image)this.FindName("YouTubeLogo");
            SearchContainer = (global::Windows.UI.Xaml.Controls.Grid)this.FindName("SearchContainer");
            SearchButton = (global::Windows.UI.Xaml.Controls.Button)this.FindName("SearchButton");
            SearchIcon = (global::Windows.UI.Xaml.Controls.Image)this.FindName("SearchIcon");
            SearchTextBox = (global::Windows.UI.Xaml.Controls.TextBox)this.FindName("SearchTextBox");
            AccountIcon = (global::Windows.UI.Xaml.Controls.Image)this.FindName("AccountIcon");
        }
    }
}



