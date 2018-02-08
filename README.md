# Localization Helper

[![Build status](https://ci.appveyor.com/api/projects/status/9quhnn68u9ao408p?svg=true)](https://ci.appveyor.com/project/gqqnbig/localizationhelper)

**Localization Helper** is a general tool that help translate file from one language to another. It's able to translate .NET .resx files, [AutoHotKey](https://autohotkey.com/) comments, [Hearts of Iron](http://store.steampowered.com/app/394360/Hearts_of_Iron_IV/) mod localization files, etc. as long as given correct pattern that extracts resource key (which is optional) and value.

Localization Helper is optimized to be used as Git hook.

## Examples 
### Localize .NET .resx files 
Install the following TortoiseGit start-commit hook

```PowerShell
cd $args[2]

# Localization

$status = git status -s
$modifiedResources = echo $status | Select-String "M (.+)(?<!\.\w\w)\.resx" #doesn't end with like pl.resx
Foreach( $modifiedResource in $modifiedResources)
{
    $relativePath = $modifiedResource.Matches[0].Groups[1].Value
    echo $relativePath
    if($status -match [Regex]::Escape("$($relativePath).pl.resx"))
    {
        Write-Host "Skip translating because $($relativePath).pl.resx has been modified."
        continue
    }


    $sourceFile= -Join($relativePath,".resx")
    $targetFile= -Join($relativePath,".pl.resx")
    $originalName= -Join($relativePath,".resx")
    # U1: show 1 line of context
    git diff -U1 $originalName |
    Translator.exe --keyValuePattern """(?<=\<data name=""""(?<k>{0})""""\s+xml[^>]+>\s*\n\s*\<value\>).+(?=\<\/value\>)"""   `
             --keyPattern \w+   `
             --source "$($sourceFile):en" `
             --target "$($targetFile):pl" `
             --apiToken  ???


    echo "`n$($relativePath).pl.resx is translated by Google" | Out-File  $args[1] -Encoding ascii -Append

}
```

### Localize AutoHotKey comments
```PowerShell
cd $args[2]

# Localization

# Compatible with Unicode file names
[Console]::OutputEncoding = [Text.Encoding]::UTF8

$status = git status -s
$modifiedResources = echo $status | Select-String "M 鼠标配合-工作.ahk" #doesn't end with like pl.ahk
Foreach( $modifiedResource in $modifiedResources)
{
    $relativePath = $modifiedResource.Matches[0].Groups[1].Value
    echo $relativePath
    if($status -match [Regex]::Escape("work.en.ahk"))
    {
        Write-Host "Skip translating because work.en.ahk has been modified."
        continue
    }



    $originalName= -Join($relativePath,".resx")
    git diff $originalName |
    Translator.exe --keyValuePattern "(?<=;[\s-[\r\n]]*)[^\r\n]+"  `
                   --source 鼠标配合-工作.ahk:zh `
                   --target work.en.ahk:en `
                   --apiToken  ???


    echo "`nwork.en.ahk is translated by Google" | Out-File  $args[1] -Encoding ascii -Append

}
```
This script translates comments in 鼠标配合-工作.ahk in Chinese to English and write to work.en.ahk.

### Localize Hearts of Icon localisation files
```PowerShell
cd $args[2]
 
# Localization
 
$status = git status -s
$modifiedResources = echo $status | Select-String "M localisation/(.+_l_)english.yml"
Foreach( $modifiedResource in $modifiedResources)
{
    $relativePath = $modifiedResource.Matches[0].Groups[1].Value
    echo $relativePath
    if($status -match [Regex]::Escape("$($relativePath)french.yml"))
    {
        Write-Host "Skip translating because $($relativePath)french.yml has been modified."
        continue
    }
 
 
    $sourceFile="localisation/$($relativePath)english.yml"
    $targetFile="localisation/$($relativePath)french.yml"
    # U1: show 1 line of context
    git diff -U1 $sourceFile |
    Translator.exe --keyValuePattern "(?<=(?<k>{0}):0\s+"").+(?=""\s*$)" `
             --keyPattern "[\w_]+\.\d+\.[td]"   `
             --source "$($sourceFile):zh-cn" `
             --target "$($targetFile):fr" `
             --apiToken  ???
 
 
    echo "`n$($targetFile) is translated by Google" | Out-File  $args[1] -Encoding ascii -Append
 
}
```
This script translates text in localisation/.+_l_english.yml in Chinese to French and write to localisation/.+_l_french.yml.




## Command Line Options

### keyPattern
It's optional. It specifies the pattern of key in [.NET regular expression](https://docs.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference). This option is used when the text in source files are in key value pairs.

If the option is omitted, `keyValuePattern` becomes _keyless_ pattern.

### keyValuePattern
If `keyPattern` is specified, this option must have `{0}` placeholder, which is used to insert `keyPattern`. 

This pattern is executed with [RegexOptions.Multiline](https://msdn.microsoft.com/library/system.text.regularexpressions.regexoptions(v=vs.110).aspx). A typical `keyValuePattern` has _Zero-width positive lookahead assertion_ and _Zero-width positive lookbehind assertion_. 

If `keyValuePattern` is a _keyless_ pattern, and `diff` is available, LocalizationHelper will try to use the text in the same line, next line, or previous line as the key to do the lookup. The goal of the keyless search and `diff` is to avoid to query Google translation API every time and repeatedly for the same source text. For instance `--keyValuePattern "(?<=;[\s-[\r\n]]*)[^\r\n]+"` matches the comments in AutoHotKey source code, meanwhile the assertion avoids `;` and non-new-line white spaces after it being fed into Google translation API.


### source
Take form of `a:b`. `b` is the language code, `a` is file name and path, relative to current directory. For instance, if the options are 

```
--source localisation\conquer-research-events_l_engggglish.yml:zh
```

Localization Helper knows the text in `localisation\conquer-research-events_l_engggglish.yml` are Chinese (zh).

### target
Similar to `source`, but applies to target file.

### diff
If option `diff` is specified, or a diff output is piped to the program, the program will perform conservative translation. 

The program will only translate keys existing in diff, avoiding changing unmoddified parts of the target file. 

