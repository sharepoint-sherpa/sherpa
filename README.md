Sherpa 
=================
# What is Sherpa
* Sherpa is a library and a command-line tool create for easy deployment of customizations and sandboxed solutions On-premises and to SharePoint Online (Office 365)
* Sherpa allows you to 
  * Deploy taxonomy (groups, term sets and terms) with known IDs 
  * Upload, activate and upgrade sandboxed solutions
  * Configure fields and content types on a site collection level
  * Activate site and web scoped features
  * Disable and re-activate selected features after a solution upgrade

# Why use Sherpa
The deployment story with especially SharePoint Online leaves a lot to be desired. If you're creating anything but apps, the deploy process is manual or requires you to write your own deploy scrips and code from scratch. Sherpa allows you to deploy your artifacts in a repeatable manner from day one, without having to spend time writing code. 
  
# How does Sherpa work? 
* The Sherpa library communicates with SharePoint exclusively through the Client Side Object Model (CSOM) 
  * Sherpa can thus configure both SharePoint on-premises and SharePoint Online
* The bundled console application uses the Sherpa library allows us to quickly get started

# Getting started
1. Clone the project from github
2. Add your fields, content types, taxonomy and site hierarchy etc through configuration, look at the sample config for guidance
3. Build the solution and navigate to the out folder, typically Sherpa.Installer\bin\debug)
4. Put any Sandboxed solutions you might have in the folder 'solutions' 
5. Start Sherpa.exe and pass the parameters 'url' and 'username' to hint where and as whom the application should connect. Add the flag --spo if and only if you are connecting to SharePoint Online. You can type 'Sherpa.exe --help' for help
6. Sherpa will authenticate the user after you provide the password. After this you can choose which action you want to perform 

# What Sherpa won't do
At the moment Sherpa is connecting to a single site collection, which means that Sherpa will not create new site collections. This also means that a site collection has to be created by an administrator up front. Sherpa also does not do any tenant administration tasks, except for setting up taxonomy.

# Known limitations
* On-premises: the application must run at one of your servers in your SharePoint farm
* On-premises: the application runs exclusively in the context of the current user

# Relevant resources
* <a href="http://www.uize.com/examples/json-prettifier.html">JSON Prettifier</a>

# Maintainers
Tarjei Ormestøyl [<a href="mailto:tarjeieo@puzzlepart.com">tarjeieo@puzzlepart.com</a>], 
Ole Kristian Mørch-Storstein [<a href="mailto:olekms@puzzlepart.com">olekms@puzzlepart.com</a>]