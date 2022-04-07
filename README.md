# GroupManager
A nifty little tool designed to allow a manager of an Active Directory Security Group to add and remove members from that group.

## Configuration
On first startup the application will open a configuration window. This window will ask the user to input the distinguished name (DN) of the organizational unit (OU) in where the application will find all users and groups it needs to know. The application can also be configured to use security groups instead of OUs. This configuration will be stored permanently in the "C:\ProgramData\Narumikazuchi\GroupManager" directory.

### Deployment
It is currently planned to implement a deployment method for enterprises, so that no actual information about the Active Directory paths and DNs is needed to be passed to an end user. But for now you would need to configure the application locally and copy the files in "C:\ProgramData\Narumikazuchi\GroupManager" over to the destination computer in order to delpoy the application.

## Navigation
The navigation of the application is conceivably easy.  
Upon startup the application already attempts to retrieve all group of which the currently logged on user is the manager of. In the event that it fails, the user can hit the "Reload List" button. Afterwards the user can just choose which group he wants to edit the members of.  
![Logo](../release/docs/MainWindow.png)  
In this window the user can either select a member to remove or choose to add a new member to the group.  
![Logo](../release/docs/GroupOverviewWindow.png)  
This window allows the end user to precisely filter the users and groups in either the configured OU or the sepcified security group.  
![Logo](../release/docs/AddMemberWindow.png)  

## Localization
The application supports the use of different display languages. Make sure to also place the associated language file in the root directory of the application. While I myself can only provide an english and a german translation, I will point to [this](https://github.com/Narumikazuchi/GroupManager_Languages) repository where you can find other translation files created by the community.  

## User Interface
The application is designed in a way, where it will adopt the current theme of windows, including the chosen accent color.
