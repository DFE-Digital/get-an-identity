workspace {

    model {

        # relationships between people and software systems

         group nonDelegates {
            intlqtsUser = person "International recognition QTS"
            engWelshQtsUser = person "English/Welsh QTS Holder"
            ittCourseUser = person "Started ITT"
            prohibitedUser = person "Ex Teacher (Prohibited)"
            pensionUser = person "Pensioner (TPS)"
            npqUser = person "Started an NPQ"
        }

         group delegates {
            schoolAdminUser = person "School Business Professional"
            ittProviderUser = person "ITT Provider"
            employer = person "Employers (in education orgs to check status)"
            sectorDeliveryOrgs = person "ITT, NPQ training providers (diff classes of SKITS,HEIs)"
            sectorAssuranceBodies = person "Appropriate bodies- checking data"
        }
        # containers / components

        softwareSystem = softwareSystem "Teacher Identity Service" "A DfE service for users to engage with their DfE Identity"{
        webapp = container "Teacher Identity Web Application" "A web application for users to engage with their DfE Identity"{
            rubyFrontEnd = component "Front End" "A front end for the service, written in Ruby"
            emailIdentitySystem = component "Email verification system" "A system that proves a user has access to an email address"
            rubyBackEnd = component "Back End" "A backend system that handles business logic for identifying users and confirming identities"
        }
        identityBroker = container "Identity Data Broker" "The data store for the identity broker. Contains attributes about people for matching purposes"{
            tags "DataStore"
            identityDataModel = component "Identity Data Model" "Relational data model used to represent an teachers identity"
            
        }
        identityBrokerAPI = container "Identity Data Broker API" "This API serves matching user attributes, given a set of other attributes about a user"
        
        govUKIntegrator = container "GOV.UK Integration Service" "A Teacher Services level integration with GOV.UK Account based on OIDC"{
            openIdConnectServer = component "OpenID Connect Server" "An implementation of an OIDC server that is configured to federate with GOV.UK Account"                       
            doorkeeper = component "Doorkeeper" "An oAuth2/OIDC provider, written in Ruby."
            omniauth = component "OmniAuth" "An oAuth2/OIDC client, written in Ruby."
        }
        }
        dqt = softwareSystem "Database of Qualified Teachers (DQT)" "A system that stores and maintains records of people in education."{
            dqtAPI = container "DQT API" "API offering some basic search functionality over the Database of Qualified Teachers (DQT)"   
            dqtCRM = container "DQT CRM" "A Microsoft CRM Dynamics implementation that contains data on people with Qualified Teacher Status (QTS) and other attributes."
        }

        # relationships between containers/components
        
        govUkAccountSSO = softwareSystem "GOV.UK Account" "A Central Government system providing SSO services to government agencies"
        intlqtsUser -> webapp "Uses"
        webapp -> govUKIntegrator "Authenticates users using"
        engWelshQtsUser -> webapp "Uses"
        ittCourseUser -> webapp "Uses"
        prohibitedUser -> webapp "Uses"
        pensionUser -> webapp "Uses"
        npqUser -> webapp "Uses"
        schoolAdminUser -> webapp "Uses on behalf of ..."
        ittProviderUser -> webapp "Uses on behalf of ..."
        employer -> webapp "Uses on behalf of ..."
        sectorDeliveryOrgs -> webapp "Uses on behalf of ..."
        sectorAssuranceBodies -> webapp "Uses on behalf of ..."

        openIdConnectServer -> govUkAccountSSO "Federates with"
        openIdConnectServer -> identityBrokerAPI "Enriches token with user attributes from"
        identityBrokerAPI -> identityBroker "Persists user attribute data to"
        identityBrokerAPI -> dqtAPI "Queries for user attributes using"
        rubyBackEnd -> identityBrokerAPI "Retrieves user information from"
        dqtAPI -> dqtCRM "Makes calls to"
        openIdConnectServer -> doorkeeper "Uses"
        openIdConnectServer -> omniauth "Uses"
        rubyFrontEnd -> rubyBackEnd "Uses"
        rubyBackEnd -> emailIdentitySystem "Confirms email using"

    }

    views { 
        theme default
    }
}