# AllJoynEcobeeDSB
An AllJoyn DSB for the Ecobee Thermostats


### Installation:

- Deploy the HeadlessAdapterApp to Windows IoT Core, and set it as a startup task.
- Use "AllJoyn Explorer" to browse to the "EcobeeDSB" device, and go to 'Configuration/org.dotMorten.Ecobee'
- Go to the `ApiKey` property and assign an Application Key created on the Ecobee developer portal. Verify that `IsApiKeySet` is now `true`. 
- Go to `RegisterApplication` and invoke the method. Take the returned `AuthorizationCode` and add the app in the Ecobee User Portal: https://www.ecobee.com/consumerportal/index.html#/my-apps/add/new
- Wait up to 30 seconds and your thermostat and sensors shoudl start working.
