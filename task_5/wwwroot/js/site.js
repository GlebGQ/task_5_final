// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var x = new Image();
x.src = '../content/images/TicTacToeX.png';
var o = new Image();
o.src = '../content/images/TicTacToeO.png';

$("#findOpponent").hide();
$("#findAnotherGame").hide();
$("#waitingForOpponent").hide();
$("#game").hide();

const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/game")
    .build();

hubConnection.start();

hubConnection.on("registerComplete", function (playerName) {
    $("#register").hide();
    $("#findOpponent").show();
});

$("#registerName").click(function () {
    hubConnection.invoke("RegisterPlayer")
});

$("#findGame").click(function () {
    hubConnection.invoke("FindOpponent");

    $("#register").hide();
    $("#findOpponent").hide();

    $("#waitingFotOpponent").show();
})

hubConnection.on("NoOpponents", function () {
    $("#gameInformation").html("<strong>Looking for an opponent!</strong>");
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
    $("#gameInformation").html('<strong>Game is over and We have a Winner!! The winner is: ' + message + '</strong>');
    $("#findAnotherGame").show();
})

hubConnection.on("OpponentDisconnected", function (message) {
    $("#gameInformation").html("<strong>Game over! " + message + " left and you won on walk over</strong>");
    $("#findAnotherGame").show();
    $("#game").hide();
});

$("#findAnotherGame").click(function () {
    hubConnection.invoke("FindOpponent");

    $("#game").empty();
    $("#game").hide();
    $("#findAnotherGame").hide();

    $("#waitingForOpponent").show();
});

