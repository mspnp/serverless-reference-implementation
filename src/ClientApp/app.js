(function () {

    // Enter Global Config Values & Instantiate ADAL AuthenticationContext
    window.config = {
        instance: 'https://login.microsoftonline.com/',
        tenant: '<Azure AD tenant name>',
        clientId: '<application id>',
        postLogoutRedirectUri: window.location.origin,
        apiId: '<api application id>',
        apiUrl: '<URL of the API Management API endpoint>/api/v1'
    };
    var authContext = new AuthenticationContext(config);

    // Get UI jQuery Objects
    var $userDisplay = $(".app-user");
    var $signInButton = $(".app-login");
    var $signOutButton = $(".app-logout");
    var $errorMessage = $(".app-error");

    // Check For & Handle Redirect From AAD After Login
    var isCallback = authContext.isCallback(window.location.hash);
    authContext.handleWindowCallback();
    $errorMessage.html(authContext.getLoginError());

    if (isCallback && !authContext.getLoginError()) {
        window.location = authContext._getItem(authContext.CONSTANTS.STORAGE.LOGIN_REQUEST);
    }

    // Check Login Status, Update UI
    var user = authContext.getCachedUser();
    if (user) {
        $userDisplay.html(user.userName);
        $userDisplay.show();
        $signInButton.hide();
        $signOutButton.show();
    } else {
        $userDisplay.empty();
        $userDisplay.hide();
        $signInButton.show();
        $signOutButton.hide();
    }

    // Register NavBar Click Handlers
    $signOutButton.click(function () {
        authContext.logOut();
    });
    $signInButton.click(function () {
        authContext.login();
    });

    function clearErrorMessage() {
        var $errorMessage = $(".app-error");
        $errorMessage.empty();
    };

    function printErrorMessage(mes) {
        var $errorMessage = $(".app-error");
        $errorMessage.html(mes);
    }

    $(".getstatus-btn").click(function () {

        $(".drone-state").hide();

        var $dataContainer = $(".data-container");
        $dataContainer.empty();
        clearErrorMessage();

        var $loading = $(".view-loading");
        $loading.show();

        // Acquire Token for Backend
        authContext.acquireToken(authContext.config.apiId, function (error, token) {

            // Handle ADAL Error
            if (error || !token) {
                printErrorMessage('ADAL Error Occurred: ' + error);
                $loading.hide();
                return;
            }

            var droneId = $("#drone-id").val();

            // Call GetStatus API
            $.ajax({
                type: "GET",
                url: config.apiUrl + "/dronestatus/" + droneId,
                headers: {
                    'Authorization': 'Bearer ' + token,
                }
            }).done(function (data) {

                // Format results
                var rows = [];

                $.each(data, function(index, element) {
                    if (index[0] != '_') {
                        rows.push("<tr><td>" + index + "</td>");
                        rows.push("<td>" + element + "</td></tr>");
                    }
                });

                // Update the UI
                $('.data-container').html(rows.join(""));
                $(".drone-state").show();

            }).fail(function () {
                printErrorMessage('Error getting drone status')
            }).always(function () {
                $loading.hide();
            });
        });
    });
}());
