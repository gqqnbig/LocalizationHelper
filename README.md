# LocalizationHelper

## Command Line Options

### keyValuePattern
Must have `{0}` placeholder, which is used to insert `keyPattern`.

### keyPattern

### fileNamePattern

### source
Take form of `a:b`. `b` is the language code, `a` is a component of file name. For instance, if the options are 

```
--fileNamePattern  "D:\mod\conquer-research\localisation\conquer-research-events_l_{0}.yml" 
--source engggglish:zh
```

The program knows the text in `D:\mod\conquer-research\localisation\conquer-research-events_l_engggglish.yml` are Chinese (zh).

### target
Similar to `source`, but applies to target file.

### diff
If option `diff` is specified, or a diff output is piped to the program, the program will perform conservative translation. 

The program will only translate keys existing in diff, avoiding changing unmoddified parts of the target file. 

## Sample Command Line
```
--keyValuePattern "(?<=(?<k>{0})\s+"")(?<v>.+)(?="")" --keyPattern [\w_]+\.\d+\.[td]:0 
--fileNamePattern  "D:\Documents\Paradox Interactive\Hearts of Iron IV\mod\conquer-research\localisation\conquer-research-events_l_{0}.yml" 
--source english:zh --target french:fr 
--diff "D:\Documents\Paradox Interactive\Hearts of Iron IV\mod\conquer-research\localisation\diff.txt"  
--apiToken <your google token>
```
