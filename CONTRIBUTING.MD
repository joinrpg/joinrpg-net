Please direct any questions to @Leotsarev or leo@joinrpg.ru

Git 
==
1. Start with forking this repository on github. 

How to build locally (on Windows)
==

1. Download [Visual Studio 2015](https://www.visualstudio.com/). Community Edition should be enough.
1. Recommended to install Github Extension (options during install). See more on https://visualstudio.github.com/.
1. Download .Net Framework 4.6 and .Net Framework 4.6 SDK.
1. Input you github credentials into Visual Studio and clone your repository.
  - Or clone repository using Git cmd client or Github for windows or whatever you like.
1. Compile VS project and observe package restoring and everything happens
1. Tap F5 and you should be there.
1. First start can take a long time while Local DB will create database for first time.
1. Some features will be disabled — it's all about integration to Remote systems — email, social services login etc. 
If you want to test this, you will need to rename Secret.config.example to Secret.config and provide correct keys.
1. Please note that you won't be able for login until you verify your email. Of course, it's not possible w/o email integration enabled.
To perform that, you will need to go to database and change `[dbo].[UserAuthDetails]` to set `[EmailConfirmed]` for corresponding user.
TODO: Check this!

How to build locally (on MacOS / Linux)
==
It's theoretically possible, but I don't know how.
