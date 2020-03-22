// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { OpenDocumentTracker } from './OpenDocumentTracker';

function launchGitStory(context: vscode.ExtensionContext) {
	const cp = require('child_process');
	cp.exec(context.asAbsolutePath('bin/GitStoryCLI.exe'), {
		cwd: vscode.workspace.rootPath
	});
}

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {

	const tracker = new OpenDocumentTracker(context);
	context.subscriptions.push(tracker);

	context.subscriptions.push(vscode.workspace.onDidSaveTextDocument((td) => {
		var allClean = true;
		tracker.documents.forEach(td => {
			allClean = allClean && !td.isDirty;
		});

		if (allClean) {
			launchGitStory(context);
		}
	}));
}

// this method is called when your extension is deactivated
export function deactivate() {}
