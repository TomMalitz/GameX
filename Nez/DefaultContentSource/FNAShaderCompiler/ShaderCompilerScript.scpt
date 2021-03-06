JsOsaDAS1.001.00bplist00?Vscript_cString.prototype.format = function()
{
    var args = arguments;
    return this.replace(/{(\d+)}/g, function(match, number)
	{
      	return typeof args[number] != 'undefined' ? args[number] : match;
    });
};

	var app = Application.currentApplication();
	app.includeStandardAdditions = true;
	var workingFolder = app.pathTo( this ).toString().replace( /[^\/]*$/, '' );
	
	var appSys = Application( 'System Events' );
	var shaderCompilerPath = '/Users/desaro/Documents/dev/MonoGame/FNAShaderCompiler/ShaderCompiler.app'
	var shaderCompilerCommand = "{0}/Contents/Frameworks/wswine.bundle/bin/wine {1}/drive_c/fxc.exe /T fx_2_0 /Fo ".format( shaderCompilerPath, shaderCompilerPath );

	// this is the command we want to emulate
	//'ShaderCompiler.app/Contents/Frameworks/wswine.bundle/bin/wine ShaderCompiler.app/drive_c/fxc.exe /T fx_2_0 /Fo out.fxb Grayscale.fx'
	
	var inputFolder = app.chooseFolder
	({
		withPrompt: 'Choose the shader source folder',
		defaultLocation: '/Users/desaro/Documents/dev/MonoGame/Nez/DefaultContentSource/effects'
	});
	var outputFolder = app.chooseFolder
	({
		withPrompt: 'Choose the shader output folder',
		defaultLocation: '/Users/desaro/Documents/dev/MonoGame/Nez/DefaultContent/FNAEffects'
	});
	
	var shaders = app.doShellScript( 'find ' + inputFolder + ' -type f -name "*.fx"' );
	shaders = shaders.split( '\r' );
	
	for( var i in shaders )
	{
		var item = shaders[i];
		var name = item.replace( /^.*[\\\/]/, '' ) + 'b';
		var command = "cd {0} && ".format( workingFolder ) + shaderCompilerCommand + "z:/{0}/{1} z:/{2}".format( outputFolder, name, item );
		app.doShellScript( command );
	}                              y jscr  ??ޭ