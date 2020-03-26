package by.yatagarasu

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.OSProcessHandler
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.components.ProjectComponent
import com.intellij.openapi.editor.Document
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.project.ProjectManager
import com.intellij.openapi.vfs.SavingRequestor
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.BulkFileListener
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import com.intellij.util.messages.MessageBusConnection
import java.io.File

fun Document.isModified(): Boolean {
    val fdm = FileDocumentManager.getInstance()
    return fdm.isFileModified(fdm.getFile(this)!!)
}

class FileSaveListenerComponent : ProjectComponent {
    var conn: MessageBusConnection? = null
    override fun initComponent() {
        conn = ApplicationManager.getApplication().messageBus.connect()
        conn?.subscribe(VirtualFileManager.VFS_CHANGES, object : BulkFileListener {
            override fun after(events: MutableList<out VFileEvent>) {
                events.forEach { e ->
                    if (e.requestor is SavingRequestor) {
                        /*
                        val fdm = FileDocumentManager.getInstance()
                        if (!fdm.unsavedDocuments
                                .fold(false) { c, d ->
                                    c || d.isModified()
                                }
                        ) {
                         */
                        ProjectManager.getInstance().openProjects.forEach { p ->
                            Runtime.getRuntime().exec(arrayOf("git", "story"), null, File(p.basePath))
                        }
                        //}
                    }
                }
            }
        })
    }

    override fun disposeComponent() {
        conn?.disconnect()
    }
}