workspace {

    model {        
         # User definitions
         group "Non-delegates" {
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

        softwareSystem = softwareSystem "Teacher Identity Service" "A DfE service for users to engage with their DfE Identity"{
        webapp = container "Teacher Identity Web Application" "A web application for users to engage with their DfE Identity"{
            findMyTrnController = component "FindTrn Controller" "A controller written in Ruby that guides a user through finding their TRN" "Ruby"
            emailComponent = component "Email verification component" "A component that proves a user has access to an email address" "Ruby"            
            omniauthInWebApp = component "OmniAuth" "An oAuth2/OIDC client, written in Ruby." "Ruby Gem"
            appViews = component "Web UI Views" "Views written in Ruby that deliver the UI" "Ruby"
        }
        identityBroker = container "Identity Data Broker" "The data store for the identity broker. Contains attributes about people for matching purposes" "Postgres/Elastic/Neo4j"{
            tags "DataStore"
        }
        identityBrokerAPI = container "Identity Data Broker API" "This API serves matching user attributes, given a set of other attributes about a user"{
            associateController = component "Associate Controller" "A controller written in Ruby that accepts and processes incoming requests to associate data attributes with records, or other attributes." "Ruby"
            findController = component "Find Controller" "A controller written in Ruby that accepts requests to search for user data, based on known user attributes" "Ruby"
            databaseLayer = component "Database Layer" "A component for interacting with the data store" "Ruby"
            DQTAPIClient = component "DQT API Client" "A component that interacts with the DQT API" "Ruby"
        }
        
        govUKIntegrator = container "GOV.UK Integration Service" "A Teacher Services level integration with GOV.UK Account based on OIDC"{
            openIdConnectServer = component "OpenID Connect Server" "An implementation of an OIDC server that is configured to federate with GOV.UK Account" "Ruby"
            doorkeeper = component "Doorkeeper" "An oAuth2/OIDC provider, written in Ruby." "Ruby Gem"
            omniauth = component "OmniAuth" "An oAuth2/OIDC client, written in Ruby." "Ruby Gem"
        }
        }
        dqt = softwareSystem "Database of Qualified Teachers (DQT)" "A system that stores and maintains records of people in education."{
            dqtAPI = container "DQT API" "API offering some basic search functionality over the Database of Qualified Teachers (DQT)" "C Sharp"  
            dqtCRM = container "DQT CRM" "A Microsoft CRM Dynamics implementation that contains data on people with Qualified Teacher Status (QTS) and other attributes." "Microsoft Dynamics CRM"
        }
        
        # direct users
        intlqtsUser -> webapp "Uses"
        engWelshQtsUser -> webapp "Uses"
        ittCourseUser -> webapp "Uses"
        prohibitedUser -> webapp "Uses"
        pensionUser -> webapp "Uses"
        npqUser -> webapp "Uses"

        # delegate users
        schoolAdminUser -> webapp "Uses on behalf of ..."
        ittProviderUser -> webapp "Uses on behalf of ..."
        employer -> webapp "Uses on behalf of ..."
        sectorDeliveryOrgs -> webapp "Uses on behalf of ..."
        sectorAssuranceBodies -> webapp "Uses on behalf of ..."

        # relationships between containers/components
        govUkAccountSSO = softwareSystem "GOV.UK Account" "A Central Government system providing SSO services to government agencies"
        omniauthInWebApp -> openIdConnectServer "Authenticates users using"
        openIdConnectServer -> govUkAccountSSO "Federates with"
        openIdConnectServer -> identityBrokerAPI "Enriches token with user attributes from"
        openIdConnectServer -> identityBrokerAPI "Updates data store with incoming attributes from GOV.UK Account"
        databaseLayer -> identityBroker "Persists user attribute data to"
        DQTAPIClient -> dqtAPI "Queries for user attributes using"
        dqtAPI -> dqtCRM "Makes calls to"
        openIdConnectServer -> doorkeeper "Uses"
        openIdConnectServer -> omniauth "Uses"
        findMyTrnController -> emailComponent
        findMyTrnController -> identityBrokerAPI "Retrieves user information from"
        associateController -> databaseLayer "Writes new association data to"
        findController -> DQTAPIClient "Queries DQT via"        
        findController -> databaseLayer "Queries additional user attributes in"
        findMyTrnController -> associateController "Associates TRNs with other coordinates"
    }

    views { 
        theme default
    }
}