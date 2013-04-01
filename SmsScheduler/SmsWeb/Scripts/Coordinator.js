
function ShowSendAll() {
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllByDiv').show(200);
    $('#TimeSeparatorSeconds').val('');
    $('#SendAllAtOnce').val(false);
    SetHighlightedButton("#SendAllByButton");
}

function ShowTimeSeparated() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').show(200);
    $('#SendAllBy').val('');
    $('#SendAllAtOnce').val(false);
    SetHighlightedButton("#TimeSeparatedButton");
}

function HideTimingOptions() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllBy').val('');
    $('#SendAllAtOnce').val(true);
    SetHighlightedButton("#AllAtOnceButton");
}

function CheckSendAll() {
    if ($('#SendAllBy').val().length == 0) {
        $('#SendAllByDiv').hide();
    } else {
        ShowSendAll();
    }
}

function CheckTimeSeparated() {
    if ($('#TimeSeparatorSeconds').val().length == 0) {
        $('#TimeSeparatedDiv').hide();
    } else {
        
        ShowTimeSeparated();
    }
}

function SetHighlightedButton(idToHighlight) {

    $('#SendAllByButton').removeClass("SelectedButton");
    $('#SendAllByButton').addClass("UnSelectedButton");
    $('#TimeSeparatedButton').removeClass("SelectedButton");
    $('#TimeSeparatedButton').addClass("UnSelectedButton");
    $('#AllAtOnceButton').removeClass("SelectedButton");
    $('#AllAtOnceButton').addClass("UnSelectedButton");
    

    $(idToHighlight).removeClass("UnSelectedButton");
    $(idToHighlight).addClass("SelectedButton");
}