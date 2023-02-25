MyApp-Tableau-Dev.postman_collection   -- A Postman collection of Tableau APIs. Load this file in PostMan to view and run the APIs.
Tableau-Dev.pstman_environment     --This is a list of variables that are used in the collection. Load this environment file in PostMan.

The variables base_url,site_id and user_name are static settings and will change only when they are updated on the server. 
Check with Tableau Admin if they don't work.

Contact the Tableau Admin for the password which is required  in the Signin-Get Token API. This API will return a token that can be used to run other APIs. 
Once a token is obtained in the reposne of Signin-Get Token API, update the token variable in the enviornment to be used by other APIs.
The token is valid for only a limited time. When the token expires, this step must be repeated.

Other variables: new_user_email, user_id, new_group_name, group_id are dynamic variables that will change from session to session. 
The values for user_id and group_id are unique ids for a User and Group respectively in Tableau. You can get these when you use the Add-User-To-Site and Add-Group to Site APIs
or use  from one the objects retrieved using Get-Users or Get-Groups APIs.


