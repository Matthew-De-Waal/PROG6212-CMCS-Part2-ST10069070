// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const EMPTY_STRING = "";
const MAX_FILE_SIZE_LIMIT = 2000000;

// This method checks if the given password is strong enough.
function IsStrongPassword(password) {
    let result = false;
    let letters = "abcdefghijklmnopqrstuvwxyz";
    let numbers = "0123456789";

    let letterCount = 0;
    let digitCount = 0;
    let symbolCount = 0;

    if (password.length >= 8) {
        for (i = 0; i < password.length; i++) {
            if (letters.includes(password.toLowerCase()[i])) {
                letterCount++;
            }

            if (numbers.includes(password.toLowerCase()[i])) {
                digitCount++;
            }

            if (!letters.includes(password.toLowerCase()[i]) && !numbers.includes(password.toLowerCase()[i])) {
                symbolCount++;
            }
        }

        if (letterCount >= 4 && digitCount >= 2 && symbolCount >= 2)
            result = true;
    }

    return result;
}

// This method gets the document type.
function getDocumentType(filename) {
    let documentType = EMPTY_STRING;

    if (filename.toLowerCase().endsWith(".pdf")) {
        documentType = "PDF";
    }
    else if (filename.toLowerCase().endsWith(".xlsx")) {
        documentType = "XLSX";
    }
    else if (filename.toLowerCase().endsWith(".png")) {
        documentType = "PNG";
    }
    else if (filename.toLowerCase().endsWith(".jpg")) {
        documentType = "JPG";
    }
    else {
        documentType = "Unknown";
    }

    return documentType;
}

// Post data
function POST_DATA(url, data, e) {
    let request = new XMLHttpRequest();
    request.open("POST", url, true);
    request.send(data);

    if (e != null) {
        request.onloadend = function () {
            e(request.status);
        }
    }
}

// Post data
function POST_DATA2(url, data, header, e) {
    let request = new XMLHttpRequest();
    request.open("POST", url, true);

    if (header != null) {
        for (let i = 0; i < header.length; i++) {
            request.setRequestHeader(header[i][0], header[i][1]);
        }
    }

    request.send(data);

    if (e != null) {
        request.onloadend = function () {
            e(request.status);
        }
    }
}

// Get data
function GET_DATA(url, header, e) {
    let request = new XMLHttpRequest();
    request.open("GET", url, true);

    if (header != null) {
        for (let i = 0; i < header.length; i++) {
            request.setRequestHeader(header[i][0], header[i][1]);
        }
    }

    request.send();

    if (e != null) {
        request.onloadend = function () {
            e(request.status, request.response);
        }
    }
}

// Get data
function GET_DATA2(url, header, e) {
    let request = new XMLHttpRequest();
    request.open("GET", url, true);
    
    if (header != null) {
        for (let i = 0; i < header.length; i++) {
            request.setRequestHeader(header[i][0], header[i][1]);
        }
    }

    request.send();

    if (e != null) {
        request.onloadend = function () {
            e(request);
        }
    }
}

// Deletes an item in an array by a given index.
function delete_item(array, index) {
    let list = [];

    for (let i = 0; i < index; i++) {
        list[i] = array[i];
    }

    for (let i = index + 1; i < array.length; i++) {
        list[i - (index + 1)] = array[i];
    }

    return list;
}

// Rounds off a number to megabytes.
function round_off_to_mb(value) {
    return (value / 1000000).toFixed(2);
}