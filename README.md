## EushullyEditor
EushullyEditor Library is a tool to make your own Eushully Script Editor.

### Features
- **Overwrite**: Rewrite all Kamidori strings.
- **Append**: Move recognized strings to the end of the script to avoid bad configurations.
- **AutoDetect**: Rewrite strings without changing unknown offsets.

### Aditional Features
- Make Fake line breaks (configured to work with Kamidori)<br>
- Get Faked Line breaks original strings
- Read-Only Mode to make japanese strings readable. sample: http://i.imgur.com/TMoGABr.jpg


### YOU NEED TO CONFIGURE IT!
I configured it to work with Kamidori. If you don't know how to configure it, you can try
contacting me and asking for help.

Good luck.

#### Release Resources:
- Hide Eushully Erros: https://github.com/guquabc/eushullyNoMsgBox


### Usage:


    //To Load
    using VNX.EushullyEditor;
    //...
    FormatOptions Config = new FormatOptions(); //Start with default config
    EushullyEditor Editor = new EushullyEditor(@"c:\sample.bin", Config); //Initialize Values
    Editor.LoadScript(); //Import Script
    string[] AllStrings = new string[Editor.Strings.Length]; //Create Variable with all entries
    
    //Convert VNX.EushullyEditor.String to String
    for (int i = 0; i < Editor.Strings.Length; i++)
        AllStrings[i] = Editor.Strings[i].getString();
    
    //...
    
    //To Save
    //Copy string[] to VNX.EushullyEditor.String[]
    for (int i = 0; i < Editor.Strings.Length; i++)
        Editor.Strings[i].setString(AllStrings[i]);
    
    byte[] OutScript = Editor.Export(); //Get script with new strings
    System.IO.File.WriteAllBytes(@"C:\sample-out.bin", OutScript); //save the sciprt


