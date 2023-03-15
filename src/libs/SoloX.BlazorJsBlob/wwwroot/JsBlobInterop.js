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

  // Save a Blob matching the blobUrl.
  saveAsFile(blobUrl, filename) {
    this.#consoleLog('save blob: ' + blobUrl)
    var link = document.createElement('a');
    link.download = filename;

    link.href = blobUrl;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
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
}

export var blobManager = new BlobManager();
