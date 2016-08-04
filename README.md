Sherpa
=================
# Update 04.08.2016: The non-existing future of Sherpa
For more or less all practical purposes, Sherpa has been replaced by the Microsoft-driven community initiative OfficePnP and the [Office PnP Provisioning Schema](https://github.com/OfficeDev/PnP-Provisioning-Schema), which can be installed using [Office PnP PowerShell](https://github.com/OfficeDev/PnP-PowerShell). There are many benefits of following this initiative, for one it has a large number of contributors which a one shop/one man initiative is unable to compete with (and it doesn't make sense to try). Being Microsoft backed, it also has the benefit of quickly adjusting to new technology from Microsoft for Office 365 and SharePoint. Puzzlepart has been using Office PnP to create customizations for Office 365 and SharePoint since mid 2015, and it's become a solid platform which we wholeheartedly support.

Today, Sherpa is still being the tool used to install Prosjektportalen, but we plan to port the installation of Prosjektportalen to OfficePnP Provisioning Schema when time and resources allow.

If you are using Sherpa for any of your projects, we encourage you to switch to Office PnP. If you have questions about this update, Office PnP or the future of Sherpa, feel free to shoot me, Tarjei Ormestøyl, an e-mail at [<a href="mailto:tarjeieo@puzzlepart.com">tarjeieo@puzzlepart.com</a>].

It was a fun ride with a lot of learning experiences, but it's great to see Sherpa be replaced by a better solution. As programmers, there is not many thing better than deleting or retiring code and software.

# What is Sherpa
* Sherpa is a library and a command-line tool create for easy deployment of customizations and sandboxed solutions On-premises and to SharePoint Online (Office 365)
* Sherpa allows you to
  * Deploy taxonomy (groups, term sets and terms) with known IDs and export to the same format
  * Upload, activate and upgrade sandboxed solutions
  * Create fields and content types
  * Configure sites (features, lists, quicklaunch, properties, upload files)
  * Import search configuration

# Why use Sherpa
The deployment story with especially SharePoint Online leaves a lot to be desired. If you're creating anything but apps, the deploy process is manual or requires you to write your own deploy scrips and code from scratch. Sherpa allows you to deploy your artifacts in a repeatable manner from day one, without having to spend time writing code.

# How does Sherpa work?
* The Sherpa library communicates with SharePoint exclusively through the Client Side Object Model (CSOM). Sherpa can thus configure both SharePoint on-premises and SharePoint Online
* The bundled console application uses the Sherpa library allows us to quickly get started

# Getting started
1. Clone the project from github
2. Add your fields, content types, taxonomy and site hierarchy etc through configuration, look at the sample config for guidance
3. In case you are doing sandboxed solutions, build the solution and navigate to the out folder, typically Sherpa.Installer\bin\debug. Put any Sandboxed solutions you might have in the folder 'solutions'
4. Start Sherpa.exe and pass the parameters 'url' and 'username' to hint where and as whom the application should connect. Add the flag --spo if and only if you are connecting to SharePoint Online. You can type 'Sherpa.exe --help' for help
5. Sherpa will authenticate the user after you provide the password. After this you can choose which action you want to perform. Sherpa also supports Windows Credential Manager

# What Sherpa won't do
At the moment Sherpa is connecting to a single site collection, which means that Sherpa will not create new site collections. This also means that a site collection has to be created by an administrator up front. Sherpa also does not do any tenant administration tasks, except for setting up taxonomy in the term store.

# Tools and resources
* <a href="http://www.uize.com/examples/json-prettifier.html">JSON Prettifier - Format JSON nicely</a>
* <a href="http://shancarter.github.io/mr-data-converter/">Mr. Data Converter - Convert Excel to JSON</a>
* <a href="http://jsonlint.com/">JSONLint - JSON validator</a>

# About
The sherpa tool is built by <a href="http://www.puzzlepart.com">Puzzlepart AS</a> as part of the <a href="https://github.com/prosjektstotte/sp-prosjektportal">Project portal for SharePoint project</a> for Asker Kommune and <a href="http://www.ks.no/kommit">KommIT</a>.

# Disclaimer
The tool is a work in progress, and not considered finished. Use at own risk. We do not recommend usage of the tool in production environments. The maintainers takes no responsibility of errors or bugs in the tool, problems caused by the tool or by usage errors.

# Maintainers
Tarjei Ormestøyl [<a href="mailto:tarjeieo@puzzlepart.com">tarjeieo@puzzlepart.com</a>],
Ole Kristian Mørch-Storstein [<a href="mailto:olekms@puzzlepart.com">olekms@puzzlepart.com</a>]
