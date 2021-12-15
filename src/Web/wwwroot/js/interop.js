var modalsConfirmBeforeClose = []

function removeModalConfirmBeforeClose(modalId){
	var pos = -1;
	do {
		pos = modalsConfirmBeforeClose.indexOf(modalId);
		if(pos > -1){
			modalsConfirmBeforeClose.splice(pos, 1);
		}
	} while(pos > -1);
}

// Listen for a custom "modalClose" event
document.addEventListener('modalClose', event => {
	var modalId = event.detail.name();
	
	for(let confId of modalsConfirmBeforeClose){
		if(confId == modalId){
			// This is a modal we want to confirm before closing.
			if(confirm("There may be unsaved changes in this form. Are you sure you want to close it?") == false){
				// They did not mean to close it, re-open the modal.
				Modal.open(event.detail.name());
			}
			break;
		}
	}
}, false);

/* Local Storage */
var sessionInstances = [];
function removeSessionInstance(id){
	var pos = -1;
	do {
		pos = -1;// Reset
		for(var i = 0; i < sessionInstances.length; i++)
		{
			if(sessionInstances[i].id == id){
				pos = i;
				break;
			}
		}
		console.log("\tpos: " + pos);
		if(pos > -1){
			sessionInstances.splice(pos, 1);
		}
	} while(pos > -1);
}

window.addEventListener('storage', () => {
	// When local storage changes, dump the list to
	// the console.
	console.log(sessionInstances);
	console.log(JSON.parse(window.localStorage.getItem('user')));
	for(let instance of sessionInstances){
		instance.ref.invokeMethodAsync("StorageChanged");
	}
  });
/* End of Local Storage */