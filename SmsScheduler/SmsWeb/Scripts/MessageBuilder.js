function CountMessageChar(input) {
    var text = input.val();
    var len = text.length;
    var maxLenthSms = 160;
    var message = maxLenthSms - len + " characters remaining.";
    $('#messageCounter').text(message);
    if (len > maxLenthSms && input.hasClass('messageInputValid')) {
        input.removeClass('messageInputValid');
        input.addClass('messageInputInvalid');
    }

    if (len <= maxLenthSms && input.hasClass('messageInputInvalid')) {
        input.removeClass('messageInputInvalid');
        input.addClass('messageInputValid');
    }
};