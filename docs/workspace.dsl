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
            findMyTrnModel = component "FindTrn Model" "Business logic at the core of the FindMyTRN service." "Ruby"
            emailComponent = component "Email verification component" "A component that proves a user has access to an email address" "Ruby"
            findMyTrnViews = component "Web UI Views" "Views written in Ruby that deliver the UI" "Ruby"
            DQTAPIClient = component "DQT API Client" "A component that interacts with the DQT API" "Ruby"
            ZendeskClient = component "ZenDesk API Client" "A component that interacts with the ZenDesk" "Ruby"
        }
        }
        dqt = softwareSystem "Database of Qualified Teachers (DQT)" "A system that stores and maintains records of people in education."{
            dqtAPI = container "DQT API" "API offering some basic search functionality over the Database of Qualified Teachers (DQT)" "C Sharp"  
            dqtCRM = container "DQT CRM" "A Microsoft CRM Dynamics implementation that contains data on people with Qualified Teacher Status (QTS) and other attributes." "Microsoft Dynamics CRM"
        }

        zendesk = softwareSystem "ZenDesk" "A SaaS Ticketing system that is used by the TRA support team." "SaaS Managed"
        
        # direct users
        intlqtsUser -> findMyTrnController "Uses"
        engWelshQtsUser -> findMyTrnController "Uses"
        ittCourseUser -> findMyTrnController "Uses"
        prohibitedUser -> findMyTrnController "Uses"
        pensionUser -> findMyTrnController "Uses"
        npqUser -> findMyTrnController "Uses"

        # delegate users
        schoolAdminUser -> findMyTrnController "Uses on behalf of"
        ittProviderUser -> findMyTrnController "Uses on behalf of"
        employer -> findMyTrnController "Uses on behalf of"
        sectorDeliveryOrgs -> findMyTrnController "Uses on behalf of"
        sectorAssuranceBodies -> findMyTrnController "Uses on behalf of"

        # relationships between containers/components        
        DQTAPIClient -> dqtAPI "Queries for user attributes using"
        ZendeskClient -> zendesk "Creates tickets in"
        dqtAPI -> dqtCRM "Makes calls to"
        findMyTrnController -> findMyTrnModel "Executes business logic inside"
        findMyTrnController -> findMyTrnViews "Interacts with users using"
        findMyTrnModel -> emailComponent "Verifies ownership of email via"
        findMyTrnModel -> DQTAPIClient "Queries DQT via"
        findMyTrnModel -> ZendeskClient "Creates helpdesk tickets via"
    }

    views { 
        theme default
    }
}