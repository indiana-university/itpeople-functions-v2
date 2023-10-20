const observerOptions = { attributes: true, childList: true, subtree: true };
		const mutationCallback = (mutationList, observer) => {
			fixDOM()
		};
		const observer = new MutationObserver(mutationCallback);
		
		window.onload = function () {
			var url = "/openapi/v3.json";

			// Pre load translate...
			if (window.SwaggerTranslator) {
				window.SwaggerTranslator.translate();
			}

			// Begin Swagger UI call region
			const ui = SwaggerUIBundle({
				url: url,
				dom_id: '#swagger-ui',
				deepLinking: true,
				validatorUrl: null,
				presets: [
					SwaggerUIBundle.presets.apis,
					SwaggerUIStandalonePreset
				],
				plugins: [
					SwaggerUIBundle.plugins.DownloadUrl
				],
				layout: "StandaloneLayout",
				onComplete: function (swaggerApi, swaggerUi) {
					if (window.SwaggerTranslator) {
						window.SwaggerTranslator.translate();
					}
                    /* Hacks to massage the DOM into compliance. */
					// Fix the DOM when Swagger-UI has finished rendering.
					fixDOM();

					// When certain DOM elements change, run fixDOM() again.
					document.querySelectorAll("div.opblock-tag-section").forEach(e => observer.observe(e, observerOptions));

                    /* End of Hacks. */
				},
				onFailure: function (data) {
					log("Unable to Load SwaggerUI");
				},
				docExpansion: "none",
				jsonEditor: false,
				// defaultModelRendering: window.siteimprove.optionDefaultModelRendering("False" === "True"), // SITEIMPROVE EDIT
				showRequestHeaders: false,
				// requestInterceptor: function (req) { req.url = req.url.replace("https://localhost/", "https://localhost:443/"); return req; }
			});
			// End Swagger UI call region

			window.ui = ui;
		};

		function fixDOM()
		{
			if(window.ui) {
				// Replace the errant <pre> tag for displaying the version
				var preVersion = document.querySelector("pre.version");
				
				if(preVersion) {
					var divVersion = document.createElement("div")
					divVersion.classList.add("preformatted-text");
					divVersion.classList.add("version");
					divVersion.innerHTML = preVersion.innerHTML;
					
					preVersion.parentNode.replaceChild(divVersion, preVersion);
				}

				// Fix the badly formed <label> tag for servers.
				var labelServer = document.querySelector('label[for="servers"]');
				if(labelServer) {
					labelServer.removeAttribute("for");
					labelServer.innerHTML = "PIck a Server\n" + labelServer.innerHTML;
				}

				// Fix a mismatch between the visible description and the aria-label for buttons to expand each api endpoint.
				// It's mad about a missing space.

				var buttonSummaryMethods = document.querySelectorAll("span.opblock-summary-method");
				for(let buttonSummaryMethod of buttonSummaryMethods)
				{
					// Append a space at the end if it does not already end with a space.
					if(buttonSummaryMethod.innerHTML.slice(-1) != " ") {
						buttonSummaryMethod.innerHTML += " ";
					}
				}

				// Fix Response JSON color contrast.
				var preExamples = document.querySelectorAll("pre.example, pre.body-param__example");
				for(let preExample of preExamples) {
					// Make the background completely black.
					preExample.style.backgroundColor = "#000000";

					// Make the highlight of an integer more contrasting
					var salmonInts = preExample.querySelectorAll('span[style="color: rgb(211, 99, 99);"]');
					for(let salmonInt of salmonInts) {
						salmonInt.style.color = "#DA7C7C";
					}
				}
			}
		}