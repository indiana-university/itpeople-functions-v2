# IT People Tasks

This project provides scheduled tasks for keeping people information, building information, and tool assignments in sync with internal IU systems.

For each task, a timer-triggered function will kick off a [Durable Function orchestration](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=csharp) at designated times throughout the day. These orchestrations enable the long-running complex workflows required to satisfy the task.

## Buildings

The [Builings](Buildings.cs) task keeps building information up-to-date with IU Facilities records. The task will fetch a list of buildings from a Denodo view maintained by the Facilities office. The task will then [upsert](https://www.postgresqltutorial.com/postgresql-upsert/) `building` records to keep them in sync.

## People

The [People](People.cs) task keeps people and department information up-to-date with IU/UITS HR records. It also provides a canonical list of employees from which new folks can be added to the directory. The task will first fetch sanitized HR records for IU employees, affiliates, and Foundation employees from the IMS Profile API. The task will then clear all existing records from the `hr_people` table and bulk-load the fetched HR records. (The `hr_people` table is essentially a "shadow table" for the IMS Profile API data.) 

Once the `hr_people` table has been bulk loaded, we [upsert](https://www.postgresqltutorial.com/postgresql-upsert/) `department` records as necessary to ensure that IT People departments are a consistent subset of IU departments.

Finally, the task will update all existing `people` records with the freshest HR position/contact/location data.

## Tools

The [Tools](Tools.cs) task keeps IT tool access in up-to-date with tool assignments in IT People. Every IT tool has an associated Active Directory (AD) group. External applications query the membership of a given group to determine whether a given person has access to that tool.

For each IT tool, the task will fetch a list of all people who have been granted access to that tool in IT People. The task will then query the membership of the AD group associated with the IT tool. The task will add to the group anyone who has been granted access in IT People but is not a member. It will likewise remove any existing group member for which there is no access grant in IT People.

In order to keep this project platform-agnostic, we use the open source [Novell.Directory.Ldap.NETStandard](https://www.nuget.org/packages/Novell.Directory.Ldap.NETStandard/) package for interacting with AD.
