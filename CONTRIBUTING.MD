Please direct any questions to t.me/leotsarev or leo@joinrpg.ru

How to build locally
==

This guide presumes that you have Windows. If you have other OS, you should replace Windows-specific
components with respective counterparts. I.e. VS → VS Code.

1. Start with forking this repository on github.
1. Download [Visual Studio 2022](https://www.visualstudio.com/). Community Edition should be enough.
1. Download .Net Core 9 SDK.
1. Input you github credentials into Visual Studio and clone your repository. Or clone repository
   using Git cmd client or Github for windows or whatever you like.
1. Install docker
1. (Optionally) Perform the following git commands to ease rebase on upstream:
  - `git remote add upstream https://github.com/joinrpg/joinrpg-net.git`
  - `git fetch upstream`
1. Run `docker compose up -d`. It will start PostgreSQL and SQL Server in background.
1. For creating DBs you can run JoinRpg.Dal.Migrate
1. Compile VS project and observe package restoring and everything happens
1. Tap F5 and you should be there.
1. Some features will be disabled — it's all about integration to Remote systems — email, social
   services login etc. If you want to test this, you will need to provide correct keys in
   appsettings.json.
1. By default, emails will be just logged.
1. First logged-in user will have his email automatically confirmed and adminstrator flag will be
   set. 

Submitting your changes
==

1. 1 branch = 1 feature.
1. Test your changes locally, etc.
1. Execute `dotnet format`, to apply project's code styles
1. Commit.
1. Rebase your changes on top of `upstream/master`
1. Open pull request via Github UI from your branch to `joinrpg/joirpg-net/master`

Telegram chats
==

We using a lot of telegram chats to talk about Joinrpg. Developer's question could be addressed
to https://t.me/joinchat/AAAAAEKdDvRjI0Z1n79EJA сhat.

Code guidelines
==

Some common code practices, used amongst this repository

- If possible, avoid the usage of regular expressions
