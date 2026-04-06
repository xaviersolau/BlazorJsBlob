// This JavaScript module provides JS side implementation of the BlobService.

class BlobManager {

  #buffers = {};
  #enableLogsOption = false;

  enableLogs(enable) {
    this.#enableLogsOption = enable;

    this.#consoleLog('enable logs: ' + enable)
  }

  // Create a buffer entry with the given Id.
  createBuffer(bufferId) {
    this.#consoleLog('create buffer: ' + bufferId)
    this.#buffers[bufferId] = {
      slices: []
    };
  }

  // Add a data slice to the buffer associated to the given bufferId.
  addToBuffer(bufferId, dataSlice, size) {
    this.#consoleLog('add slice to buffer: ' + dataSlice.length + ' ' + size)
    if (dataSlice.length != size) {
      this.#buffers[bufferId].slices.push(dataSlice.subarray(0, size));
    } else {
      this.#buffers[bufferId].slices.push(dataSlice);
    }
  }

  // Add a base64 data slice to the buffer associated to the given bufferId.
  addToBufferB64(bufferId, dataSliceBase64, size) {
    var data = window.atob(dataSliceBase64);

    var dataSlice = new Uint8Array(data.length);
    for (var i = 0; i < data.length; i++) {
      dataSlice[i] = data.charCodeAt(i);
    }

    this.addToBuffer(bufferId, dataSlice, size);
  }

  // Delete the buffer associated to the given bufferId.
  deleteBuffer(bufferId) {
    this.#consoleLog('delete buffer: ' + bufferId)
    delete this.#buffers[bufferId];
  }

  // Create a Blob from the buffer associated to the given bufferId.
  createBlob(bufferId, type) {
    this.#consoleLog('create blob from buffer id: ' + bufferId)
    var bufferByteArrays = this.#buffers[bufferId].slices;

    var blob = new Blob(bufferByteArrays, { type: type });
    var blobUrl = URL.createObjectURL(blob);
    return blobUrl;
  }

  // Delete Blob matching the blobUrl.
  deleteBlob(blobUrl) {
    this.#consoleLog('delete blob ' + blobUrl)
    URL.revokeObjectURL(blobUrl);
  }

  // Download Url file.
  saveAsFile(url, filename) {
    this.#consoleLog('save url: ' + url)

    this.#consoleLog('user agent: ' + navigator.userAgent)

    try {

      const isIOS = /iP(ad|hone|od)/.test(navigator.userAgent);
      const isSafari = /Safari/.test(navigator.userAgent) && !/Chrome/.test(navigator.userAgent);

      const isBrokenDownload = isIOS || isSafari;

      this.#consoleLog('isBrokenDownload: ' + isBrokenDownload)

      if (url.toLowerCase().startsWith("blob:") || !isBrokenDownload) {
        this.#downloadUrl(url, filename);
      }
      else {
        // no blob && isBrokenDownload
        fetch(url)
          .then(res => res.blob())
          .then(blob => {
            const urlToSave = URL.createObjectURL(blob);

            this.#downloadUrl(urlToSave, filename);

            // delay revocation slightly to avoid race conditions
            setTimeout(() => {
              URL.revokeObjectURL(urlToSave);
            }, 100);
          });
      }

    } catch (err) {
      this.#consoleError(err);

      window.open(url, '_blank');
    }
  }

  #downloadUrl(urlToSave, filename) {
    const a = document.createElement('a');
    a.href = urlToSave;
    a.download = filename;

    document.body.appendChild(a);

    a.click();

    a.remove();
  }

  // ping method to tell Blazor world that JS interoperability is alive.
  ping() {
    this.#consoleLog('ping')
    return true;
  }

  #consoleLog(message) {
    if (this.#enableLogsOption) {
      console.log(message);
    }
  }
  #consoleError(message) {
    if (this.#enableLogsOption) {
      console.error(message);
    }
  }
}

export var blobManager = new BlobManager();
