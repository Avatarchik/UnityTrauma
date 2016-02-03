@MenuItem("Assets/Build Preloader")
static function ExportResourcePreloader () {

	System.IO.Directory.CreateDirectory("AssetBundles");

    BuildPipeline.PushAssetDependencies();
    BuildPipeline.BuildPlayer(["Assets/Scenes/pregame.unity","Assets/Scenes/mainmenu.unity","Assets/Scenes/loading.unity"], "AssetBundles/TraumaPreloader.unity3d",BuildTarget.WebPlayer, BuildOptions.ShowBuiltPlayer);
    BuildPipeline.PopAssetDependencies();	
}

@MenuItem("Assets/Build AllScenes")
static function ExportResourceAllScenes () {

	System.IO.Directory.CreateDirectory("AssetBundles");

    BuildPipeline.PushAssetDependencies();
    BuildPipeline.BuildPlayer(["Assets/Scenes/trauma_05.unity","Assets/Scenes/testimonial.unity","Assets/Scenes/endGame.unity"], "AssetBundles/TraumaGame.unity3d", BuildTarget.WebPlayer, BuildOptions.BuildAdditionalStreamedScenes);      
    BuildPipeline.PopAssetDependencies();	
}