
# Service Taxonomy Editor

## Introduction

This project is a headless content management system (CMS), that synchronises content into a graph database. It's being created by the [UK government's](https://www.gov.uk/) [National Careers Service](https://nationalcareers.service.gov.uk/) to manage careers related content.

## Developer Environment Setup

The solution is built using .NET Core 3.1, a Neo4j database and a RDBMS database. You should be able to develop and run the solution on Windows, macOS or Linux.

### Set Up A Neo4j Graph

Download [Neo4j desktop](https://neo4j.com/download/), install and run it.

Click 'Add Graph', then 'Create a Local Graph'. Enter a graph name, set the password to `ESCO`, select the latest version in the dropdown, then click 'Create'. Once the graph is created, click the 'Start' button.

To perform interactive queries against the graph, once the graph is Active, click 'Manage' and then on the next page, click 'Open Browser'. If you're unfamiliar with the browser or Neo4j in general, check out the [docs](https://neo4j.com/developer/neo4j-browser/).

#### Prepare the ESCO graph

1) Download the [ESCO Classifications Full RDF](https://ec.europa.eu/esco/portal/download).
2) Download the latest [Neosemantics plugin](https://github.com/neo4j-labs/neosemantics/releases) and copy the jar to the plugins directory (Manage > Open Folder > Plugins). There is a [Neosemantics user guide](https://neo4j.com/docs/labs/nsmntx/current/).
3) Install the APOC plugin (Manage > Plugins > Install)
4) For full search synonym support, add the following to your neo4j.conf file, replacing localhost and port as necessary:
```
ncs.occupation_synonyms_file_url=https://localhost:44346/graphsync/synonyms/occupation/synonyms.txt
ncs.skill_synonyms_file_url=https://localhost:44346/graphsync/synonyms/skill/synonyms.txt
```
5) Restart the graph instance
6) Enable the browser setting "Enable multi statement query editor"
7) Paste and execute the following into the desktop graph browser (Manage > Open Browser).
```
CREATE CONSTRAINT n10s_unique_uri ON (r:Resource) ASSERT r.uri IS UNIQUE
call n10s.graphconfig.init( { handleMultival: "ARRAY", multivalPropList: ["http://www.w3.org/2004/02/skos/core#altLabel", "http://www.w3.org/2004/02/skos/core#hiddenLabel"] });
call n10s.nsprefixes.add("esco", "http://data.europa.eu/esco/model#");
call n10s.nsprefixes.add("iso-thes", "http://purl.org/iso25964/skos-thes#");
call n10s.nsprefixes.add("skosxl", "http://www.w3.org/2008/05/skos-xl#");
call n10s.nsprefixes.add("esco-rp", "http://data.europa.eu/esco/regulated-professions/");

// Import the ESCO data (replace the path to the ESCO file downloaded in step 1) - takes approx 3 to 5 mins
call n10s.rdf.import.fetch("file:///%USERPROFILE%/downloads/esco_v1.0.3.ttl","Turtle", { languageFilter: "en" });

// Fix some anomalies by executing
MATCH(n:esco__Occupation)
WHERE EXISTS(n.skos__hiddenLabel)
SET n.skos__altLabel = n.skos__hiddenLabel, n.skos__hiddenLabel = null
RETURN n
```
To confirm the import worked ok, check the schema using
```
CALL db.schema.visualization()
```

#### Create Content Database

Setup a database to store the content. Any database with a .NET ADO provider is supported. For example, [setup a Azure SQL Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started?tabs=azure-portal). To quickly try it out, you can run a local Sqlite database with no initial setup necessary.

If you choose to use a SQL Server or Azure SQL database, ensure that the connection string enables multiple active result sets (MARS), by including `MultipleActiveResultSets=True`. If you go through the set-up process again (after deleting `App_Data`), you'll need to clear down the Azure SQL / SQL Server database, otherwise you'll get the error `invalid serial number for shell descriptor`.

### Run And Configure Website

Clone the [GitHub repo](https://github.com/SkillsFundingAgency/dfc-servicetaxonomy-editor) and open the solution `DFC.ServiceTaxonomyEditor.sln` in your favourite .NET Core supporting IDE, such as [Visual Studio](https://visualstudio.microsoft.com/), [Visual Code](https://code.visualstudio.com/) or [Rider](https://www.jetbrains.com/rider/).

Set `DFC.ServiceTaxonomy.Editor` as the startup project.

Add an `appsettings.Development.json` file and populate it with the following config, inserting the appropriate `TopicEndpoint` and `AegSasKey` values:

```
{
    "ContentApiPrefix": "http://localhost:7071/api/execute",
    "Neo4j": {
        "Endpoints": [
            {
                "Uri": "bolt://localhost:7687",
                "Username": "neo4j",
                "Password": "ESCO3",
                "Enabled": true
            }
        ]
    },
    "EventGrid": {
        "PublishEvents": "true",
        "Topics": [
            {
                "ContentType": "*",
                "TopicEndpoint": "",
                "AegSasKey": ""
            }
        ]
    }
}
```

Make sure the password matches the password you created the graph with. This file is git ignored, so won't be checked in.

You'll need to update the `appsettings.json` located within `App_Data\Sites\Default`, after the site has been created but BEFORE you import recipes.

If you are using SQLite for local development, paste in the following, changing the localhost port for the Content API if necessary:

```
{
  "DatabaseProvider": "Sqlite",
  "ContentApiPrefix" : "http://localhost:7071/api/execute"
}
```

Run or debug the `DFC.ServiceTaxonomy.Editor` project, which should launch the Setup page. Populate the page as follows, and click Finish Setup. (This runs the site using a local Sqlite database.)

![Service Taxonomy Editor Setup](/Images/EditorSetup.png)
*Note: this step will become unnecessary as the solution evolves.*

You should then be directed to the log in page. Enter the username and password you've just set up. If you have the memory of a goldfish, delete the DFC.ServiceTaxonomy.Editor\App_Data folder and start again.

### Import Content

There's no need to import any content, unless you want to create pages (see 'Import Content To Enable Page Creation' below). If you wish to import a job profile data set, follow the 'Import Job Profiles' instructions.

#### Import Job Profiles

Before running the import, copy the latest `DFC.ServiceTaxonomy.Neo4j/Java Plugins/target/ncs-service-taxonomy-plugins-x.x.x-*.jar` into the Neo4J plugins directory, and restart the graph.

To import content, including the National Careers Service job profiles, import a master recipe from the `DFC.ServiceTaxonomy.Editor/MasterRecipes` folder, using using Configuration .. Import/Export .. Package Import

For the very first import, choose one of the graph mutator master recipes. You can import a full or subset of job profiles, by selecting an appropriate master recipe.

#### Import Content To Enable Page Creation

To be able to create pages, you need to import GetJobProfiles\ContentRecipes\Taxonomies.recipe.json (using Configuration .. Import/Export .. Package Import).

## Troubleshooting Builds

We track the latest preview packages of Orchard Core (OC), so that we pick up new features and fixes straight away.

The downside of this, is that occasionally, build or package issues and bugs are introduced in the OC package set. That means that any build can fail without being caused by any of our changes.

When that happens, check the [recent checkins to OC](https://github.com/OrchardCMS/OrchardCore/commits/dev), and if there is a suspect checkin (or build failure, which is often a packaging issue), replace all occurrences of referencing the packages using `-rc2-*` to a previous version and retry. If there is an issue, it's a good idea to [report it to the OC developers](https://gitter.im/OrchardCMS/OrchardCore).

## User Guide

Here's the [user guide](User%20Documentation/README.md).

## Resources

[Orchard Core gitter channel](https://gitter.im/OrchardCMS/OrchardCore)
