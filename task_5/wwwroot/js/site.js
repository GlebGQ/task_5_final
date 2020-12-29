// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var x = new Image();
x.src = '/content/images/TicTacToeX.png';
var o = new Image();
o.src = '/content/images/TicTacToeO.png';

$("#findAnotherGame").hide();
$("#game").hide();

const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/game")
    .build();

hubConnection.start().then(() => {
    setTimeout(x => {
        hubConnection.invoke("RegisterPlayer")
        hubConnection.invoke("ConnectOpponents");
    }, 1);
});

hubConnection.on("FoundOpponent", function (message) {
    $("#waitingForOpponent").hide();
    $("#gameInformation").html("You're playing againt " + message);
    $("#game").show();
    for (var i = 0; i < 9; ++i) {
        $("#game").append("<span id='" + i + "' class='box'>");
    }
});

$("#game").on("click", ".box", function (event) {
    if ($(this).hasClass("marked")) return;
    let id = event.target.id;
    hubConnection.invoke("Play", id);
});

$("#modalCloseButton").on('click', function () {
    window.location.replace(lobbyurl);
})

hubConnection.on("AddMarkerPlacement", function (info) {
    if (info.opponentName !== userName) {
        $("#" + info.markerPosition).addClass("mark2");
        $("#" + info.markerPosition).addClass("marked");
        $("#gameInformation").html("<strong>You're up!</strong>");
    } else {
        $("#" + info.markerPosition).addClass("mark1");
        $("#" + info.markerPosition).addClass("marked");
        $("#gameInformation").html("<strong>Waiting for the opponent to make a move!</strong>");
    }
    $('#debug').append('<li>Marker was placed by ' + info.opponentName + ' at position ' + info.markerPosition + '</li>');

});

hubConnection.on("GameOver", function (message) {
    $("#resultInfo").html("The winner is " + message);
    $("#resultModal").modal('show');
})

hubConnection.on("Draw", function (message) {
    $("#resultInfo").html("It's a draw!");
    $("#resultModal").modal('show');
})

hubConnection.on("RedirectToLobby", function () {
    $("#resultInfo").html("You are disconnected from the game.");
    $("#resultModal").modal('show');
});

hubConnection.on("OpponentDisconnected", function (message) {
    $("#resultInfo").html("<strong>Game over! " + message + " left and you won on walk over</strong>");
    $("#resultModal").modal('show');
});