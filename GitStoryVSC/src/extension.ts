// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { OpenDocumentTracker } from './OpenDocumentTracker';

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {

	const tracker = new OpenDocumentTracker(context);
	context.subscriptions.push(tracker);

	context.subscriptions.push(vscode.workspace.onDidSaveTextDocument((td) => {
		console.log(`did save ${td.uri}:${vscode.workspace.textDocuments.values.length}`);
	}));
}

// this method is called when your extension is deactivated
export function deactivate() {}
