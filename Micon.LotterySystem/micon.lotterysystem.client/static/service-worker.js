self.addEventListener("push", (event) => {
	if (!event.data) return;

	const data = event.data.json();

	const title = data.title || "通知";
	
	const options = {
		body: data.body,
		icon: "./favicon.png",
		data: data.url
	};

	event.waitUntil(
		self.registration.showNotification(title, options)
	);
});

self.addEventListener("notificationclick", (event) => {
	event.notification.close();
	const url = event.notification.data || "/";

	event.waitUntil(
		clients.matchAll({ type: "window", includeUncontrolled: true })
			.then((clientList) => {
				for (const client of clientList) {
					if (client.url === url && "focus" in client) {
						return client.focus();
					}
				}
				if (clients.openWindow) {
					return clients.openWindow(url);
				}
			})
	);
});