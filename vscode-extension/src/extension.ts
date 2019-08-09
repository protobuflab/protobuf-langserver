import * as vscode from 'vscode';
import { ProtoLogger } from './ProtoLogger';
import { Trace } from 'vscode-languageclient';
import { ProtoLanguageServerClient } from './ProtoLanguageServerClient';
import { resolveLanguageServerPath } from './ResolveLanguageServerPath';
import { ProtobufDocumentManager } from './ProtobufDocumentManager';

export async function activate(context: vscode.ExtensionContext) {
	const verbosity = Trace.Messages;
	const logger = new ProtoLogger(verbosity);
	const languageServerPath = resolveLanguageServerPath();
	const protoConfigSection = vscode.workspace.getConfiguration('proto');
	const debugLanguageServer = protoConfigSection.get<boolean>('debug');
	const debug = debugLanguageServer ? debugLanguageServer : false;
	const languageServerClient = new ProtoLanguageServerClient(languageServerPath, debug, logger);

	const documentManager = new ProtobufDocumentManager(languageServerClient, logger);

	const localRegistrations: vscode.Disposable[] = [];

	const onStopRegistration = languageServerClient.onStop(() => {
		localRegistrations.forEach(r => r.dispose());
		localRegistrations.length = 0;
	});

	languageServerClient.onStart(async () => {
		// Once the language server is started this will be called, it's where we can register all of our
		// bits with VSCode for them to function properly.
		// languageServerClient.onRequest('getAllDocuments', () => documentManager.getAllDocuments());
		localRegistrations.push(
			documentManager.register()
		);
	});

	languageServerClient.onStarted(async () => {
		await documentManager.initialize();
	});

	await languageServerClient.start();

	context.subscriptions.push(languageServerClient, onStopRegistration);
}

// this method is called when your extension is deactivated
export function deactivate() {}