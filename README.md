# This is still work in progress and you will face issues if you decide to try it for yourself

# Discord Unfolded (by David Golunski)
This plugin allows you to browse your discord servers, join channels and see who is inside the voice channels.
This plugin is designed for bigger StreamDeck models, so you can use the entire space to visualize and control your discord client.

## Credits and Support
Thank you to [BarRaider](https://barraider.com/) and their [Streamdeck Tools](https://github.com/BarRaider/streamdeck-tools) which allowed quicker and easier development.
Some Icons have been taken from [uxwing](https://uxwing.com/).

If you like the plugin please consider [supporting me via PayPal](https://www.paypal.com/donate/?hosted_button_id=ZN3URG59JBRVJ).   
This will allow me to keep the applications alive for a little bit longer :)


## Initial Setup
This Plugin Requires you to create your own Discord Application inside the Discord Developer Portal.

1. Drag and Drop the _"Global Settings"_ action on any button in the Streamdeck. You should see fields like _"Client ID"_ and _"Client Secret"_ that we need to fill out, before the plugin can work
2. Go to the [--> Discord Developer Site <--](https://discord.com/developers/applications) and click on __"New Application"__
3. Select a name you want, agree to the Terms of Service and click on __"Create"__
4. Once created, please select the tab called _"OAuth2"_. You should see a field called _"CLIENT ID"_ at the top of the page.
It should look something like this: 1337102485017595966  
Copy this _"CLIENT ID"_ and paste it into the _"Client ID"_ field in the _"Global Settings"_ Action
5. Click on the _"Reset Secret"_. This generates a new secret. Copy the generated Secret and paste it to the _"Client Secret"_ field in the _"Global Settings"_ Action.
6. Click on the __"Add Redirect"__ button and paste the following URL into the field:  
https://127.0.0.1:7393/callback  
After this a _"Save"_ button should have appeared. Make sure to save the changes.
7. Now you should be ready. But as mentioned at the top, the plugin is still work in progress. To get the application to work now you need to do the following steps in addition:  
7.1. Make sure that you have not used any other action other than "Global Settings". Otherwise the settings will not get saved.  
7.2. Restart the Streamdeck Program
8. When asked by discord to authorize, you will need to click on "Authorize" twice 